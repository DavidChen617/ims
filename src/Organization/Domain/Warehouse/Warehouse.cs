using Davish.Result;
using SharedKernel;

namespace Domain.Warehouse;

public sealed class Warehouse: AggregateRoot
{
    public string Name { get; private set; } = null!;

    public static Result<Warehouse> Create(string name)
    {
        var warehouse = new Warehouse { Id = Guid.CreateVersion7(), Name = name };
        return warehouse;
    }
}
