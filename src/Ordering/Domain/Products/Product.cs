using Davish.Result;
using SharedKernel;

namespace Domain.Products;

public sealed class Product : AggregateRoot
{
    public string ProductNo { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public ProductUnit Unit { get; private set; } = null!;
    public decimal Price { get; private set; }

    public static Result<Product> Create(string productNo, string name, ProductUnit unit, decimal price)
    {
        return new Product
        {
            Id = Guid.CreateVersion7(),
            ProductNo = productNo,
            Name = name,
            Unit = unit,
            Price = price
        };
    }
}

public sealed class ProductUnit
{
    public string Name { get; private set; } = null!;

    public static Result<ProductUnit> Create(string name)
    {
        var unit = new ProductUnit
        {
            Name = name
        };

        return unit;
    }
}
