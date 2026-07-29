using Domain.Warehouse;

namespace Organization.UnitTest;

public class WarehouseTests
{
    [Fact]
    public void GivenValidName_WhenCreated_ThenPropertiesAreSetCorrectly()
    {
        var result = Warehouse.Create("Test Warehouse");

        Assert.True(result.IsSuccess);
        var warehouse = result.Value;
        Assert.NotEqual(Guid.Empty, warehouse.Id);
        Assert.Equal("Test Warehouse", warehouse.Name);
    }
}
