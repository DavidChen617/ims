using Davish.Result;

namespace Domain.Products;

// Value Object
public sealed record Price
{
    public decimal Value { get; private init; }

    public static Result<Price> Create(decimal value)
    {
        if (value < 0)
            return new Error("Price.Invalid", "Price cannot be negative");

        return new Price { Value = value };
    }
}
