using Davish.Result;
using SharedKernel;

namespace Domain.Users;

public sealed class User : AggregateRoot
{
    public Guid? WarehouseId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public Role Role { get; private set; }

    public static Result<User> Register(Guid warehouseId, string name, string username, string passwordHash, Role role)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            WarehouseId =  warehouseId,
            Name = name,
            Username = username,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        return user;
    }
    
    public static Result<User> RegisterAdmin(string name, string username, string passwordHash, Role role)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Username = username,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        return user;
    }
}
