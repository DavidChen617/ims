using Dapper;
using Domain.Users;
using Domain.Warehouse;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Organization.IntegrationTest;

public class UserRepositoryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GivenNewAdminUser_WhenAddedThenQueriedByUsername_ThenReturnsTheSameUser()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrganizationUnitOfWork>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var username = $"user-{Guid.CreateVersion7()}";
        var user = User.RegisterAdmin("Test Admin", username, "hashed-password", Role.Admin).Value;

        try
        {
            await userRepository.AddAsync(user, CancellationToken.None);
            var found = await userRepository.GetByUsername(username, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(user.Id, found.Value.Id);
            Assert.Equal(username, found.Value.Username);
            Assert.Null(found.Value.WarehouseId);
            Assert.Equal(Role.Admin, found.Value.Role);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from users where id = @Id", new { user.Id });
        }
    }

    [Fact]
    public async Task GivenNewAdminUser_WhenAddedThenQueriedById_ThenReturnsTheSameUser()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrganizationUnitOfWork>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var username = $"user-{Guid.CreateVersion7()}";
        var user = User.RegisterAdmin("Test Admin", username, "hashed-password", Role.Admin).Value;

        try
        {
            await userRepository.AddAsync(user, CancellationToken.None);
            var found = await userRepository.GetByIdAsync(user.Id, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(user.Id, found.Value.Id);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from users where id = @Id", new { user.Id });
        }
    }

    [Fact]
    public async Task GivenUsersInDifferentWarehouses_WhenListedByWarehouseId_ThenOnlyReturnsThatWarehousesUsers()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrganizationUnitOfWork>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var warehouseRepository = scope.ServiceProvider.GetRequiredService<IWarehouseRepository>();

        var warehouseA = Warehouse.Create($"warehouse-{Guid.CreateVersion7()}").Value;
        var warehouseB = Warehouse.Create($"warehouse-{Guid.CreateVersion7()}").Value;

        var userInA = User.Register(warehouseA.Id, "User A", $"user-{Guid.CreateVersion7()}", "hashed-password",
            Role.WarehouseUser).Value;
        var userInB = User.Register(warehouseB.Id, "User B", $"user-{Guid.CreateVersion7()}", "hashed-password",
            Role.WarehouseUser).Value;

        try
        {
            await warehouseRepository.AddAsync(warehouseA, CancellationToken.None);
            await warehouseRepository.AddAsync(warehouseB, CancellationToken.None);
            await userRepository.AddAsync(userInA, CancellationToken.None);
            await userRepository.AddAsync(userInB, CancellationToken.None);

            var listedA = await userRepository.ListAsync(warehouseA.Id, CancellationToken.None);

            Assert.True(listedA.IsSuccess);
            Assert.Contains(listedA.Value, u => u.Id == userInA.Id);
            Assert.DoesNotContain(listedA.Value, u => u.Id == userInB.Id);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from users where id in (@A, @B)",
                new { A = userInA.Id, B = userInB.Id });
            await unitOfWork.Connection.ExecuteAsync("delete from warehouse where id in (@A, @B)",
                new { A = warehouseA.Id, B = warehouseB.Id });
        }
    }

    [Fact]
    public async Task GivenMissingUsername_WhenQueried_ThenReturnsFailure()
    {
        using var scope = factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var found = await userRepository.GetByUsername(
            $"does-not-exist-{Guid.CreateVersion7()}", CancellationToken.None);

        Assert.False(found.IsSuccess);
    }
}
