namespace Application.Abstracts;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Role { get; }
    string Name { get; }
    string Username { get; }
    Guid? WarehouseId { get; }
}
