using Dapper;
using Domain.Products;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.IntegrationTest;

public class ProductRepositoryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GivenNewProduct_WhenAddedThenQueriedByNo_ThenReturnsTheSameProduct()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrderingUnitOfWork>();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var unitName = $"unit-{Guid.CreateVersion7()}";
        var unit = ProductUnit.Create(unitName).Value;
        await productRepository.AddUnitAsync(unit, CancellationToken.None);

        var productNo = $"P-{Guid.CreateVersion7()}";
        var product = Product.Create(productNo, "Test Product", unit, 12.5m).Value;

        try
        {
            await productRepository.AddAsync(product, CancellationToken.None);
            var found = await productRepository.GetByNoAsync(productNo, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(product.Id, found.Value.Id);
            Assert.Equal(productNo, found.Value.ProductNo);
            Assert.Equal("Test Product", found.Value.Name);
            Assert.Equal(unitName, found.Value.Unit.Name);
            Assert.Equal(12.5m, found.Value.Price.Value);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from products where id = @Id", new { product.Id });
            await unitOfWork.Connection.ExecuteAsync("delete from product_units where name = @unitName", new { unitName });
        }
    }

    [Fact]
    public async Task GivenMissingProductNo_WhenQueried_ThenReturnsFailure()
    {
        using var scope = factory.Services.CreateScope();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var found = await productRepository.GetByNoAsync($"does-not-exist-{Guid.CreateVersion7()}", CancellationToken.None);

        Assert.False(found.IsSuccess);
    }

    [Fact]
    public async Task GivenSomeExistingAndSomeMissingIds_WhenCheckedForExistingIds_ThenReturnsOnlyTheExistingOnes()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrderingUnitOfWork>();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var unitName = $"unit-{Guid.CreateVersion7()}";
        var unit = ProductUnit.Create(unitName).Value;
        await productRepository.AddUnitAsync(unit, CancellationToken.None);

        var product = Product.Create($"P-{Guid.CreateVersion7()}", "Test Product", unit, 1m).Value;
        var missingId = Guid.CreateVersion7();

        try
        {
            await productRepository.AddAsync(product, CancellationToken.None);

            var result = await productRepository.GetExistingIdsAsync([product.Id, missingId], CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Contains(product.Id, result.Value);
            Assert.DoesNotContain(missingId, result.Value);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from products where id = @Id", new { product.Id });
            await unitOfWork.Connection.ExecuteAsync("delete from product_units where name = @unitName", new { unitName });
        }
    }

    [Fact]
    public async Task GivenUnitUsedByAProduct_WhenCheckedIfInUse_ThenReturnsTrue()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrderingUnitOfWork>();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var unitName = $"unit-{Guid.CreateVersion7()}";
        var unit = ProductUnit.Create(unitName).Value;
        await productRepository.AddUnitAsync(unit, CancellationToken.None);

        var product = Product.Create($"P-{Guid.CreateVersion7()}", "Test Product", unit, 1m).Value;

        try
        {
            await productRepository.AddAsync(product, CancellationToken.None);

            var inUse = await productRepository.IsUnitInUseAsync(unitName, CancellationToken.None);

            Assert.True(inUse.IsSuccess);
            Assert.True(inUse.Value);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from products where id = @Id", new { product.Id });
            await unitOfWork.Connection.ExecuteAsync("delete from product_units where name = @unitName", new { unitName });
        }
    }

    [Fact]
    public async Task GivenUnitNotUsedByAnyProduct_WhenDeleted_ThenSucceeds()
    {
        using var scope = factory.Services.CreateScope();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var unitName = $"unit-{Guid.CreateVersion7()}";
        var unit = ProductUnit.Create(unitName).Value;
        await productRepository.AddUnitAsync(unit, CancellationToken.None);

        var deleteResult = await productRepository.DeleteUnitAsync(unitName, CancellationToken.None);
        var found = await productRepository.GetUnitByNameAsync(unitName, CancellationToken.None);

        Assert.True(deleteResult.IsSuccess);
        Assert.False(found.IsSuccess);
    }
}
