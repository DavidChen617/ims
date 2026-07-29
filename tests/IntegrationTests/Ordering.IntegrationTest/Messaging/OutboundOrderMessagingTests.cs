using System.Text;
using Application;
using Application.Abstracts;
using Application.Outbound;
using Dapper;
using Davish.Sendr;
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

    // 驗證 ListOutboundHistoryQueryHandler 的快取邏輯真的會發生「cache hit」時不會爆炸 ——
    // Result<TValue> 的建構子是 internal,一開始的實作直接快取整個 Result<TValue>,
    // cache hit 反序列化時一定會丟 NotSupportedException。連續呼叫兩次同一個查詢、
    // 中間不做任何會讓快取失效的操作,第二次一定會是 cache hit,藉此逼出這條路徑。
    [Fact]
    public async Task GivenSameHistoryQueryTwice_WhenSecondCallHitsCache_ThenDeserializesWithoutThrowing()
    {
        var (unitName, product) = await CreateProductAsync();
        var order = OutboundOrder
            .Create($"OUT-{Guid.CreateVersion7()}", Guid.CreateVersion7(), Guid.CreateVersion7(), "Requester", [(product.Id, 1)])
            .Value;

        order.MarkReserved();
        order.Confirm(Guid.CreateVersion7(), "Confirmer");
        await AddOrderAsync(order);

        try
        {
            var first = await QueryAllWarehousesHistoryAsync();
            var second = await QueryAllWarehousesHistoryAsync();

            Assert.Contains(first.Items, dto => dto.Id == order.Id);
            Assert.Contains(second.Items, dto => dto.Id == order.Id);
            Assert.Equal(first.TotalCount, second.TotalCount);
        }
        finally
        {
            await CleanUpAsync(order.Id, product.Id, unitName);
        }
    }

    // 驗證「全倉庫」(WarehouseId = null,Admin 沒篩選查詢)的歷程快取,在任何一個倉庫的
    // 出貨單被確認時也會一併失效 —— 不是只有那個倉庫自己的 key 才會被清掉。
    // 這個測試如果拿掉 OutboundOrderConfirmedDomainEventHandler 裡對
    // HistoryCacheKey.AllWarehouses 的那次 DeleteByPrefixAsync,應該會失敗
    // (第二次查詢會回傳跟第一次一樣的舊快取,看不到剛剛才 Confirm 的訂單)。
    [Fact]
    public async Task GivenConfirmedOrder_WhenAllWarehousesHistoryWasCached_ThenCacheIsInvalidated()
    {
        var (unitName, product) = await CreateProductAsync();
        var order = OutboundOrder
            .Create($"OUT-{Guid.CreateVersion7()}", Guid.CreateVersion7(), Guid.CreateVersion7(), "Requester", [(product.Id, 1)])
            .Value;

        order.MarkReserved();
        await AddOrderAsync(order);

        try
        {
            var beforeConfirm = await QueryAllWarehousesHistoryAsync();
            Assert.DoesNotContain(beforeConfirm.Items, dto => dto.Id == order.Id);

            using (var scope = factory.Services.CreateScope())
            {
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var confirmResult = await sender.SendAsync(new ConfirmOutboundCommand(order.Id), CancellationToken.None);

                Assert.True(confirmResult.IsSuccess);
            }

            var afterConfirm = await QueryAllWarehousesHistoryAsync();

            Assert.Contains(afterConfirm.Items, dto => dto.Id == order.Id);
        }
        finally
        {
            await CleanUpAsync(order.Id, product.Id, unitName);
        }
    }

    private async Task<PagedResult<OutboundHistoryDto>> QueryAllWarehousesHistoryAsync()
    {
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(
            new ListOutboundHistoryQuery(
                WarehouseId: null, OrderNo: null, Status: null, RequestedFrom: null, RequestedTo: null,
                CompletedFrom: null, CompletedTo: null, ProductNo: null, ProductName: null, Unit: null,
                RequestedByName: null, ConfirmedByName: null, Page: 1, Size: 50),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        return result.Value;
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
        var cacher = scope.ServiceProvider.GetRequiredService<ICacher>();

        await unitOfWork.Connection.ExecuteAsync(
            "delete from outbound_order_items where outbound_order_id = @orderId", new { orderId });
        await unitOfWork.Connection.ExecuteAsync("delete from outbound_orders where id = @orderId", new { orderId });
        await unitOfWork.Connection.ExecuteAsync("delete from products where id = @productId", new { productId });
        await unitOfWork.Connection.ExecuteAsync("delete from product_units where name = @unitName", new { unitName });

        // 「全倉庫」歷程快取是所有測試共用的同一個 key,不清掉的話,這個測試留下的
        // cache hit 會讓下一個測試撈到已經被刪掉的訂單 id。
        await cacher.DeleteByPrefixAsync($"outbound-history:{HistoryCacheKey.AllWarehouses}", CancellationToken.None);
    }
}
