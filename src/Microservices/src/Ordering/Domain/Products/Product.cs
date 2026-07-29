using Davish.Result;
using SharedKernel;

namespace Domain.Products;

public sealed class Product : AggregateRoot
{
    public string ProductNo { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public ProductUnit Unit { get; private set; } = null!;
    public Price Price { get; private set; } = null!;

    public static Result<Product> Create(string productNo, string name, ProductUnit unit, decimal price)
    {
        var priceResult = Price.Create(price);

        if (!priceResult.IsSuccess)
            return priceResult.Error;

        return new Product
        {
            Id = Guid.CreateVersion7(),
            ProductNo = productNo,
            Name = name,
            Unit = unit,
            Price = priceResult.Value
        };
    }
}

public sealed class ProductUnit
{
    public string Name { get; private set; } = null!;

    public static Result<ProductUnit> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new Error("ProductUnit.Invalid", "Unit name cannot be empty");

        var unit = new ProductUnit
        {
            Name = name
        };

        return unit;
    }
}
