using Domain.Users;

namespace Organization.UnitTest;

public class UserTests
{
    [Fact]
    public void GivenValidInputs_WhenRegistered_ThenPropertiesAreSetCorrectly()
    {
        var warehouseId = Guid.CreateVersion7();

        var result = User.Register(warehouseId, "Test User", "test-username", "hashed-password",
            Role.WarehouseUser);

        Assert.True(result.IsSuccess);
        var user = result.Value;
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(warehouseId, user.WarehouseId);
        Assert.Equal("Test User", user.Name);
        Assert.Equal("test-username", user.Username);
        Assert.Equal("hashed-password", user.PasswordHash);
        Assert.Equal(Role.WarehouseUser, user.Role);
    }

    [Fact]
    public void GivenValidInputs_WhenRegisteredAsAdmin_ThenWarehouseIdIsNull()
    {
        var result = User.RegisterAdmin("Test Admin", "test-admin", "hashed-password", Role.Admin);

        Assert.True(result.IsSuccess);
        var user = result.Value;
        Assert.Null(user.WarehouseId);
        Assert.Equal(Role.Admin, user.Role);
    }
}
