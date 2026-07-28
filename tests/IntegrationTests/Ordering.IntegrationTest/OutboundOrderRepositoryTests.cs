using Dapper;
using Domain.OutboundOrders;
using Domain.Products;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.IntegrationTest;

public class OutboundOrderRepositoryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GivenNewOrderWithItems_WhenAddedThenQueried_ThenReturnsTheSameOrderAndItems()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrderingUnitOfWork>();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var outboundOrderRepository = scope.ServiceProvider.GetRequiredService<IOutboundOrderRepository>();

        (string unitName, Product product) = await CreateProductAsync(productRepository);
        var warehouseId = Guid.CreateVersion7();
        var order = OutboundOrder
            .Create($"OUT-{Guid.CreateVersion7()}", warehouseId, Guid.CreateVersion7(), "Requester", [(product.Id, 5)])
            .Value;

        try
        {
            await outboundOrderRepository.AddAsync(order, CancellationToken.None);
            var found = await outboundOrderRepository.GetByIdAsync(order.Id, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(order.OrderNo, found.Value.OrderNo);
            Assert.Equal(warehouseId, found.Value.WarehouseId);
            Assert.Equal(OutboundOrderStatus.Processing, found.Value.Status);
            Assert.Single(found.Value.Items);
            Assert.Equal(product.Id, found.Value.Items[0].ProductId);
            Assert.Equal(5, found.Value.Items[0].Quantity);
        }
        finally
        {
            await CleanUpAsync(unitOfWork, order.Id, product.Id, unitName);
        }
    }

    [Fact]
    public async Task GivenExistingOrder_WhenMarkedReservedAndSaved_ThenPersistsPendingStatus()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrderingUnitOfWork>();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var outboundOrderRepository = scope.ServiceProvider.GetRequiredService<IOutboundOrderRepository>();

        var (unitName, product) = await CreateProductAsync(productRepository);
        var order = OutboundOrder
            .Create($"OUT-{Guid.CreateVersion7()}", Guid.CreateVersion7(), Guid.CreateVersion7(), "Requester", [(product.Id, 1)])
            .Value;

        try
        {
            await outboundOrderRepository.AddAsync(order, CancellationToken.None);
            order.MarkReserved();
            await outboundOrderRepository.SaveAsync(order, CancellationToken.None);

            var found = await outboundOrderRepository.GetByIdAsync(order.Id, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(OutboundOrderStatus.Pending, found.Value.Status);
        }
        finally
        {
            await CleanUpAsync(unitOfWork, order.Id, product.Id, unitName);
        }
    }

    [Fact]
    public async Task GivenMissingId_WhenQueried_ThenReturnsFailure()
    {
        using var scope = factory.Services.CreateScope();
        var outboundOrderRepository = scope.ServiceProvider.GetRequiredService<IOutboundOrderRepository>();

        var found = await outboundOrderRepository.GetByIdAsync(Guid.CreateVersion7(), CancellationToken.None);

        Assert.False(found.IsSuccess);
    }

    private static async Task<(string UnitName, Product Product)> CreateProductAsync(IProductRepository productRepository)
    {
        var unitName = $"unit-{Guid.CreateVersion7()}";
        var unit = ProductUnit.Create(unitName).Value;
        await productRepository.AddUnitAsync(unit, CancellationToken.None);

        var product = Product.Create($"P-{Guid.CreateVersion7()}", "Test Product", unit, 1m).Value;
        await productRepository.AddAsync(product, CancellationToken.None);

        return (unitName, product);
    }

    private static async Task CleanUpAsync(IOrderingUnitOfWork unitOfWork, Guid orderId, Guid productId, string unitName)
    {
        await unitOfWork.Connection.ExecuteAsync(
            "delete from outbound_order_items where outbound_order_id = @orderId", new { orderId });
        await unitOfWork.Connection.ExecuteAsync("delete from outbound_orders where id = @orderId", new { orderId });
        await unitOfWork.Connection.ExecuteAsync("delete from products where id = @productId", new { productId });
        await unitOfWork.Connection.ExecuteAsync("delete from product_units where name = @unitName", new { unitName });
    }
}
