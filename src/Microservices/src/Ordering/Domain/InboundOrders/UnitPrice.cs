using Davish.Result;

namespace Domain.InboundOrders;

// Value Object
public sealed record UnitPrice
{
    public decimal Value { get; private init; }

    public static Result<UnitPrice> Create(decimal value)
    {
        if (value < 0)
            return new Error("UnitPrice.Invalid", "Unit price cannot be negative");

        return new UnitPrice { Value = value };
    }
}
