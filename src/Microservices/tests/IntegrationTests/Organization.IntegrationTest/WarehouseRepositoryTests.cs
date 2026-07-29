using Dapper;
using Domain.Warehouse;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Organization.IntegrationTest;

public class WarehouseRepositoryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GivenNewWarehouse_WhenAddedThenQueriedByName_ThenReturnsTheSameWarehouse()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrganizationUnitOfWork>();
        var warehouseRepository = scope.ServiceProvider.GetRequiredService<IWarehouseRepository>();

        var name = $"warehouse-{Guid.CreateVersion7()}";
        var warehouse = Warehouse.Create(name).Value;

        try
        {
            await warehouseRepository.AddAsync(warehouse, CancellationToken.None);
            var found = await warehouseRepository.GetByNameAsync(name, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(warehouse.Id, found.Value.Id);
            Assert.Equal(name, found.Value.Name);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from warehouse where id = @Id", new { warehouse.Id });
        }
    }

    [Fact]
    public async Task GivenNewWarehouse_WhenAddedThenQueriedById_ThenReturnsTheSameWarehouse()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrganizationUnitOfWork>();
        var warehouseRepository = scope.ServiceProvider.GetRequiredService<IWarehouseRepository>();

        var name = $"warehouse-{Guid.CreateVersion7()}";
        var warehouse = Warehouse.Create(name).Value;

        try
        {
            await warehouseRepository.AddAsync(warehouse, CancellationToken.None);
            var found = await warehouseRepository.GetByIdAsync(warehouse.Id, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(warehouse.Id, found.Value.Id);
            Assert.Equal(name, found.Value.Name);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from warehouse where id = @Id", new { warehouse.Id });
        }
    }

    [Fact]
    public async Task GivenNewWarehouse_WhenListed_ThenTheListIncludesIt()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrganizationUnitOfWork>();
        var warehouseRepository = scope.ServiceProvider.GetRequiredService<IWarehouseRepository>();

        var name = $"warehouse-{Guid.CreateVersion7()}";
        var warehouse = Warehouse.Create(name).Value;

        try
        {
            await warehouseRepository.AddAsync(warehouse, CancellationToken.None);
            var listed = await warehouseRepository.ListAsync(CancellationToken.None);

            Assert.True(listed.IsSuccess);
            Assert.Contains(listed.Value, w => w.Id == warehouse.Id && w.Name == name);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from warehouse where id = @Id", new { warehouse.Id });
        }
    }

    [Fact]
    public async Task GivenMissingName_WhenQueried_ThenReturnsFailure()
    {
        using var scope = factory.Services.CreateScope();
        var warehouseRepository = scope.ServiceProvider.GetRequiredService<IWarehouseRepository>();

        var found = await warehouseRepository.GetByNameAsync(
            $"does-not-exist-{Guid.CreateVersion7()}", CancellationToken.None);

        Assert.False(found.IsSuccess);
    }
}
