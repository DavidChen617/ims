using Application;

namespace Api.Identity;

public enum Role
{
    Admin,
    WarehouseAdmin,
    WarehouseUser
}

public sealed class CurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Username { get; set; } = null!;
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}
