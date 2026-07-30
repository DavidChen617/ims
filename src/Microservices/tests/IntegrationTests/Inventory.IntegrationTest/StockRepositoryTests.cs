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
    public async Task GivenConcurrentReservations_WhenTotalDemandExceedsStock_ThenNeverOversells()
    {
        var productId = Guid.CreateVersion7();
        var warehouseId = Guid.CreateVersion7();
        var stock = Stock.Create(productId, warehouseId).Value;
        stock.Increase(100);

        using (var seedScope = factory.Services.CreateScope())
        {
            await seedScope.ServiceProvider.GetRequiredService<IStockRepository>()
                .AddAsync(stock, CancellationToken.None);
        }

        const int concurrentRequests = 30;
        const int quantityPerRequest = 5;

        try
        {
            var succeeded = await Task.WhenAll(Enumerable.Range(0, concurrentRequests).Select(async _ =>
            {
                using var scope = factory.Services.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

                var found = await repository.GetByProductAndWarehouseAsync(productId, warehouseId, CancellationToken.None);
                var reserveResult = found.Value.TryReserve(quantityPerRequest);

                if (!reserveResult.IsSuccess)
                    return false;

                // 記憶體檢查當下讀到的可能已經是過期的庫存,真正的把關在原子條件式 UPDATE:
                // 若寫入當下庫存已經不夠,SaveAsync 會回傳失敗,這裡才是「有沒有真的搶到」的唯一依據。
                var saveResult = await repository.SaveAsync(found.Value, CancellationToken.None);
                return saveResult.IsSuccess;
            }));

            var successCount = succeeded.Count(s => s);

            using var verifyScope = factory.Services.CreateScope();
            var final = await verifyScope.ServiceProvider.GetRequiredService<IStockRepository>()
                .GetByProductAndWarehouseAsync(productId, warehouseId, CancellationToken.None);

            Assert.True(final.Value.Quantity >= 0, $"庫存變成負數: {final.Value.Quantity}");
            // 每個「SaveAsync 回傳成功」的請求都該扣到庫存 —— 如果中間有 lost update,
            // 這個數字對不起來,代表有請求誤以為自己搶到了,但寫入被別人蓋掉。
            Assert.Equal(100 - successCount * quantityPerRequest, final.Value.Quantity);
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            var unitOfWork = cleanupScope.ServiceProvider.GetRequiredService<IInventoryUnitOfWork>();
            await unitOfWork.Connection.ExecuteAsync("delete from stocks where id = @Id", new { stock.Id });
        }
    }

    [Fact]
    public async Task GivenConcurrentIncreases_WhenSaved_ThenNoLostUpdates()
    {
        var productId = Guid.CreateVersion7();
        var warehouseId = Guid.CreateVersion7();
        var stock = Stock.Create(productId, warehouseId).Value;

        using (var seedScope = factory.Services.CreateScope())
        {
            await seedScope.ServiceProvider.GetRequiredService<IStockRepository>()
                .AddAsync(stock, CancellationToken.None);
        }

        const int concurrentRequests = 30;
        const int quantityPerRequest = 5;

        try
        {
            await Task.WhenAll(Enumerable.Range(0, concurrentRequests).Select(async _ =>
            {
                using var scope = factory.Services.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

                var found = await repository.GetByProductAndWarehouseAsync(productId, warehouseId, CancellationToken.None);
                found.Value.Increase(quantityPerRequest);

                var saveResult = await repository.SaveAsync(found.Value, CancellationToken.None);
                Assert.True(saveResult.IsSuccess);
            }));

            using var verifyScope = factory.Services.CreateScope();
            var final = await verifyScope.ServiceProvider.GetRequiredService<IStockRepository>()
                .GetByProductAndWarehouseAsync(productId, warehouseId, CancellationToken.None);

            // 如果用絕對值蓋回去(舊寫法),併發的多個 Increase 會互相蓋掉彼此的結果,總和對不起來。
            Assert.Equal(concurrentRequests * quantityPerRequest, final.Value.Quantity);
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            var unitOfWork = cleanupScope.ServiceProvider.GetRequiredService<IInventoryUnitOfWork>();
            await unitOfWork.Connection.ExecuteAsync("delete from stocks where id = @Id", new { stock.Id });
        }
    }

    [Fact]
    public async Task GivenConcurrentReleaseReservations_WhenSaved_ThenNoLostUpdates()
    {
        var productId = Guid.CreateVersion7();
        var warehouseId = Guid.CreateVersion7();
        var stock = Stock.Create(productId, warehouseId).Value;
        stock.Increase(100);
        stock.TryReserve(100);

        using (var seedScope = factory.Services.CreateScope())
        {
            await seedScope.ServiceProvider.GetRequiredService<IStockRepository>()
                .AddAsync(stock, CancellationToken.None);
        }

        const int concurrentRequests = 20;
        const int quantityPerRequest = 5;

        try
        {
            await Task.WhenAll(Enumerable.Range(0, concurrentRequests).Select(async _ =>
            {
                using var scope = factory.Services.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IStockRepository>();

                var found = await repository.GetByProductAndWarehouseAsync(productId, warehouseId, CancellationToken.None);
                found.Value.ReleaseReservation(quantityPerRequest);

                var saveResult = await repository.SaveAsync(found.Value, CancellationToken.None);
                Assert.True(saveResult.IsSuccess);
            }));

            using var verifyScope = factory.Services.CreateScope();
            var final = await verifyScope.ServiceProvider.GetRequiredService<IStockRepository>()
                .GetByProductAndWarehouseAsync(productId, warehouseId, CancellationToken.None);

            Assert.Equal(concurrentRequests * quantityPerRequest, final.Value.Quantity);
            Assert.Equal(100 - concurrentRequests * quantityPerRequest, final.Value.CumulativeShipped);
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            var unitOfWork = cleanupScope.ServiceProvider.GetRequiredService<IInventoryUnitOfWork>();
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
