using Dapper;
using Domain.Stocks;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTest;

public class StockRepositoryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GivenNewStock_WhenAddedThenQueried_ThenReturnsTheSameStock()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IInventoryUnitOfWork>();
        var repository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

        var productId = Guid.CreateVersion7();
        var warehouseId = Guid.CreateVersion7();
        var stock = Stock.Create(productId, warehouseId).Value;
        stock.Increase(10);

        try
        {
            await repository.AddAsync(stock, CancellationToken.None);
            var found = await repository.GetByProductAndWarehouseAsync(productId, warehouseId, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(stock.Id, found.Value.Id);
            Assert.Equal(10, found.Value.Quantity);
            Assert.Equal(0, found.Value.CumulativeShipped);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from stocks where id = @Id", new { stock.Id });
        }
    }

    [Fact]
    public async Task GivenExistingStock_WhenReservedAndSaved_ThenPersistsNewQuantityAndCumulativeShipped()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IInventoryUnitOfWork>();
        var repository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

        var productId = Guid.CreateVersion7();
        var warehouseId = Guid.CreateVersion7();
        var stock = Stock.Create(productId, warehouseId).Value;
        stock.Increase(10);

        try
        {
            await repository.AddAsync(stock, CancellationToken.None);

            stock.TryReserve(4);
            await repository.SaveAsync(stock, CancellationToken.None);

            var found = await repository.GetByProductAndWarehouseAsync(productId, warehouseId, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(6, found.Value.Quantity);
            Assert.Equal(4, found.Value.CumulativeShipped);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from stocks where id = @Id", new { stock.Id });
        }
    }

    [Fact]
    public async Task GivenMissingProductAndWarehouse_WhenQueried_ThenReturnsFailure()
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

        var found = await repository.GetByProductAndWarehouseAsync(
            Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.False(found.IsSuccess);
    }
}
