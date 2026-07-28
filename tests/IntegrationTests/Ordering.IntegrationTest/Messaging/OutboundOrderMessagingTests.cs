using System.Text;
using Dapper;
using Domain.OutboundOrders;
using Domain.Products;
using Infrastructure.Persistence;
using MessageContract.OutboundOrders;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.IntegrationTest.Messaging;

// 端對端測試,對著一個真實(用完就丟)的 Kafka broker:直接塞一筆原始訊息到
// "inventory.events",模擬 Inventory 的 OutboxProcessor 會產生的訊息,等 app 自己的
// IntegrationEventConsumer 有機會處理完之後,再驗證最後的 DB 狀態(OutboundOrder 的狀態轉換)。
public class OutboundOrderMessagingTests(KafkaCustomWebApplicationFactory factory)
    : IClassFixture<KafkaCustomWebApplicationFactory>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task GivenProcessingOrder_WhenInventoryReservedArrives_ThenTransitionsToPending()
    {
        var (unitName, product) = await CreateProductAsync();
        var order = OutboundOrder
            .Create($"OUT-{Guid.CreateVersion7()}", Guid.CreateVersion7(), Guid.CreateVersion7(), "Requester", [(product.Id, 1)])
            .Value;

        await AddOrderAsync(order);

        using var producer = KafkaTestSupport.CreateProducer(factory.BootstrapServers);

        try
        {
            await KafkaTestSupport.ProduceAsync(
                producer,
                KafkaCustomWebApplicationFactory.InventoryTopic,
                nameof(OutboundInventoryReservedIntegrationEvent),
                new OutboundInventoryReservedIntegrationEvent(order.Id, order.WarehouseId));

            var status = await PollForStatusAsync(order.Id, OutboundOrderStatus.Pending);

            Assert.Equal(OutboundOrderStatus.Pending, status);
        }
        finally
        {
            await CleanUpAsync(order.Id, product.Id, unitName);
        }
    }

    [Fact]
    public async Task GivenProcessingOrder_WhenInventoryReservationFailedArrives_ThenTransitionsToRejected()
    {
        var (unitName, product) = await CreateProductAsync();
        var order = OutboundOrder
            .Create($"OUT-{Guid.CreateVersion7()}", Guid.CreateVersion7(), Guid.CreateVersion7(), "Requester", [(product.Id, 1)])
            .Value;

        await AddOrderAsync(order);

        using var producer = KafkaTestSupport.CreateProducer(factory.BootstrapServers);

        try
        {
            await KafkaTestSupport.ProduceAsync(
                producer,
                KafkaCustomWebApplicationFactory.InventoryTopic,
                nameof(OutboundInventoryReservationFailedIntegrationEvent),
                new OutboundInventoryReservationFailedIntegrationEvent(order.Id, order.WarehouseId, [product.Id]));

            var status = await PollForStatusAsync(order.Id, OutboundOrderStatus.Rejected);

            Assert.Equal(OutboundOrderStatus.Rejected, status);
        }
        finally
        {
            await CleanUpAsync(order.Id, product.Id, unitName);
        }
    }

    [Fact]
    public async Task GivenMalformedPayload_WhenConsumed_ThenRoutedToDeadLetterTopic()
    {
        var key = Guid.NewGuid().ToString();

        using var producer = KafkaTestSupport.CreateProducer(factory.BootstrapServers);
        using var consumer = KafkaTestSupport.CreateConsumer(
            factory.BootstrapServers, KafkaCustomWebApplicationFactory.InventoryDeadLetterTopic, $"test-dlq-{key}");

        var malformed = new Confluent.Kafka.Message<string, string>
        {
            Key = key,
            Value = "{ not valid json",
            Headers = new Confluent.Kafka.Headers
            {
                { "EventType", Encoding.UTF8.GetBytes(nameof(OutboundInventoryReservedIntegrationEvent)) }
            }
        };

        await producer.ProduceAsync(KafkaCustomWebApplicationFactory.InventoryTopic, malformed);

        var deadLettered = KafkaTestSupport.ConsumeRawMatching(consumer, r => r.Message.Key == key, Timeout);

        Assert.NotNull(deadLettered);
        Assert.Equal("{ not valid json", deadLettered.Message.Value);
        Assert.True(deadLettered.Message.Headers.TryGetLastBytes("Error", out _));
        Assert.True(deadLettered.Message.Headers.TryGetLastBytes("FailedAt", out _));
    }

    private async Task<(string UnitName, Product Product)> CreateProductAsync()
    {
        using var scope = factory.Services.CreateScope();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var unitName = $"unit-{Guid.CreateVersion7()}";
        var unit = ProductUnit.Create(unitName).Value;
        await productRepository.AddUnitAsync(unit, CancellationToken.None);

        var product = Product.Create($"P-{Guid.CreateVersion7()}", "Test Product", unit, 1m).Value;
        await productRepository.AddAsync(product, CancellationToken.None);

        return (unitName, product);
    }

    private async Task AddOrderAsync(OutboundOrder order)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboundOrderRepository>();

        await repository.AddAsync(order, CancellationToken.None);
    }

    // handler 的 DB 寫入發生在 app 自己的背景 consumer 執行緒上 —— 這裡沒有後續的
    // outbound Kafka 訊息可以觀察,所以只能用 polling 的方式等結果狀態出現。
    private async Task<OutboundOrderStatus?> PollForStatusAsync(Guid orderId, OutboundOrderStatus expected)
    {
        var deadline = DateTime.UtcNow + Timeout;

        while (DateTime.UtcNow < deadline)
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IOutboundOrderRepository>();

            var found = await repository.GetByIdAsync(orderId, CancellationToken.None);
            if (found.IsSuccess && found.Value.Status == expected)
                return found.Value.Status;

            await Task.Delay(TimeSpan.FromMilliseconds(300));
        }

        return null;
    }

    private async Task CleanUpAsync(Guid orderId, Guid productId, string unitName)
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrderingUnitOfWork>();

        await unitOfWork.Connection.ExecuteAsync(
            "delete from outbound_order_items where outbound_order_id = @orderId", new { orderId });
        await unitOfWork.Connection.ExecuteAsync("delete from outbound_orders where id = @orderId", new { orderId });
        await unitOfWork.Connection.ExecuteAsync("delete from products where id = @productId", new { productId });
        await unitOfWork.Connection.ExecuteAsync("delete from product_units where name = @unitName", new { unitName });
    }
}
