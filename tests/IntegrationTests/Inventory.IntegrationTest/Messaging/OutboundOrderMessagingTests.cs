using System.Text;
using Dapper;
using Domain.Stocks;
using Infrastructure.Persistence;
using MessageContract;
using MessageContract.OutboundOrders;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTest.Messaging;

// 端對端測試,對著一個真實(用完就丟)的 Kafka broker:直接塞一筆原始訊息到
// "ordering.events",模擬 Ordering 的 OutboxProcessor 會產生的訊息,再驗證
// app 自己的 IntegrationEventConsumer/OutboxProcessor 這條管線(透過落在
// "inventory.events"/DLQ topic 上的結果訊息)以及最後的 DB 狀態。
public class OutboundOrderMessagingTests(KafkaCustomWebApplicationFactory factory)
    : IClassFixture<KafkaCustomWebApplicationFactory>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task GivenSufficientStock_WhenOutboundOrderCreatedArrives_ThenReservesAndPublishesReserved()
    {
        var productId = Guid.CreateVersion7();
        var warehouseId = Guid.CreateVersion7();
        var outboundOrderId = Guid.CreateVersion7();

        var stockId = await SeedStockAsync(productId, warehouseId, quantity: 10);

        using var producer = KafkaTestSupport.CreateProducer(factory.BootstrapServers);
        using var consumer = KafkaTestSupport.CreateConsumer(
            factory.BootstrapServers, KafkaCustomWebApplicationFactory.InventoryTopic, $"test-{outboundOrderId}");

        try
        {
            await KafkaTestSupport.ProduceAsync(
                producer,
                KafkaCustomWebApplicationFactory.OrderingTopic,
                nameof(OutboundOrderCreatedIntegrationEvent),
                new OutboundOrderCreatedIntegrationEvent(
                    outboundOrderId, warehouseId, "測試倉庫",
                    [new EnrichedOrderItem(productId, "P-001", "測試商品", "個", 4)]));

            var reserved = KafkaTestSupport.ConsumeMatching<OutboundInventoryReservedIntegrationEvent>(
                consumer,
                nameof(OutboundInventoryReservedIntegrationEvent),
                e => e.OutboundOrderId == outboundOrderId,
                Timeout);

            Assert.NotNull(reserved);
            Assert.Equal(warehouseId, reserved.WarehouseId);

            var stock = await GetStockAsync(stockId);
            Assert.Equal(6, stock.Quantity);
            Assert.Equal(4, stock.CumulativeShipped);
            Assert.Equal("P-001", stock.ProductNo);
            Assert.Equal("測試商品", stock.ProductName);
            Assert.Equal("個", stock.Unit);
            Assert.Equal("測試倉庫", stock.WarehouseName);
        }
        finally
        {
            await DeleteStockAsync(stockId);
        }
    }

    [Fact]
    public async Task GivenInsufficientStock_WhenOutboundOrderCreatedArrives_ThenPublishesReservationFailedAndLeavesStockUntouched()
    {
        var productId = Guid.CreateVersion7();
        var warehouseId = Guid.CreateVersion7();
        var outboundOrderId = Guid.CreateVersion7();

        var stockId = await SeedStockAsync(productId, warehouseId, quantity: 2);

        using var producer = KafkaTestSupport.CreateProducer(factory.BootstrapServers);
        using var consumer = KafkaTestSupport.CreateConsumer(
            factory.BootstrapServers, KafkaCustomWebApplicationFactory.InventoryTopic, $"test-{outboundOrderId}");

        try
        {
            await KafkaTestSupport.ProduceAsync(
                producer,
                KafkaCustomWebApplicationFactory.OrderingTopic,
                nameof(OutboundOrderCreatedIntegrationEvent),
                new OutboundOrderCreatedIntegrationEvent(
                    outboundOrderId, warehouseId, "測試倉庫",
                    [new EnrichedOrderItem(productId, "P-001", "測試商品", "個", 5)]));

            var failed = KafkaTestSupport.ConsumeMatching<OutboundInventoryReservationFailedIntegrationEvent>(
                consumer,
                nameof(OutboundInventoryReservationFailedIntegrationEvent),
                e => e.OutboundOrderId == outboundOrderId,
                Timeout);

            Assert.NotNull(failed);
            Assert.Contains(productId, failed.InsufficientProductIds);

            var stock = await GetStockAsync(stockId);
            Assert.Equal(2, stock.Quantity);
            Assert.Equal(0, stock.CumulativeShipped);
            // 這次預留嘗試整個被 rollback 了,所以 SetDisplayInfo 從來沒有真正寫進去。
            Assert.Null(stock.ProductNo);
        }
        finally
        {
            await DeleteStockAsync(stockId);
        }
    }

    [Fact]
    public async Task GivenMalformedPayload_WhenConsumed_ThenRoutedToDeadLetterTopic()
    {
        var key = Guid.NewGuid().ToString();

        using var producer = KafkaTestSupport.CreateProducer(factory.BootstrapServers);
        using var consumer = KafkaTestSupport.CreateConsumer(
            factory.BootstrapServers, KafkaCustomWebApplicationFactory.OrderingDeadLetterTopic, $"test-dlq-{key}");

        var malformed = new Confluent.Kafka.Message<string, string>
        {
            Key = key,
            Value = "{ not valid json",
            Headers = new Confluent.Kafka.Headers
            {
                { "EventType", Encoding.UTF8.GetBytes(nameof(OutboundOrderCreatedIntegrationEvent)) }
            }
        };

        await producer.ProduceAsync(KafkaCustomWebApplicationFactory.OrderingTopic, malformed);

        var deadLettered = KafkaTestSupport.ConsumeRawMatching(
            consumer, r => r.Message.Key == key, Timeout);

        Assert.NotNull(deadLettered);
        Assert.Equal("{ not valid json", deadLettered.Message.Value);
        Assert.True(deadLettered.Message.Headers.TryGetLastBytes("Error", out _));
        Assert.True(deadLettered.Message.Headers.TryGetLastBytes("FailedAt", out _));
    }

    private async Task<Guid> SeedStockAsync(Guid productId, Guid warehouseId, int quantity)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

        var stock = Stock.Create(productId, warehouseId).Value;
        stock.Increase(quantity);
        await repository.AddAsync(stock, CancellationToken.None);

        return stock.Id;
    }

    private async Task<StockRow> GetStockAsync(Guid stockId)
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IInventoryUnitOfWork>();

        if (unitOfWork.Connection.State != System.Data.ConnectionState.Open)
            await unitOfWork.Connection.OpenAsync();

        return await unitOfWork.Connection.QuerySingleAsync<StockRow>(
            "select quantity, cumulative_shipped, product_no, product_name, unit, warehouse_name " +
            "from stocks where id = @Id", new { Id = stockId });
    }

    private sealed record StockRow(
        int Quantity, int CumulativeShipped, string? ProductNo, string? ProductName, string? Unit, string? WarehouseName);

    private async Task DeleteStockAsync(Guid stockId)
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IInventoryUnitOfWork>();

        if (unitOfWork.Connection.State != System.Data.ConnectionState.Open)
            await unitOfWork.Connection.OpenAsync();

        await unitOfWork.Connection.ExecuteAsync("delete from stocks where id = @Id", new { Id = stockId });
    }
}
