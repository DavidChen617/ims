using Domain.Products;

namespace Ordering.UnitTest;

public class ProductTests
{
    [Fact]
    public void GivenValidInputs_WhenCreated_ThenPropertiesAreSetCorrectly()
    {
        var unit = ProductUnit.Create("pcs").Value;

        var result = Product.Create("P-001", "Widget", unit, 9.99m);

        Assert.True(result.IsSuccess);
        var product = result.Value;
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("P-001", product.ProductNo);
        Assert.Equal("Widget", product.Name);
        Assert.Equal(unit, product.Unit);
        Assert.Equal(9.99m, product.Price.Value);
    }
}

public class ProductUnitTests
{
    [Fact]
    public void GivenValidName_WhenCreated_ThenPropertiesAreSetCorrectly()
    {
        var result = ProductUnit.Create("box");

        Assert.True(result.IsSuccess);
        Assert.Equal("box", result.Value.Name);
    }
}
