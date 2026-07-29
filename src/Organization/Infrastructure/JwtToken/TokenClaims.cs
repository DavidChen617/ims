using Domain.Users;
using Domain.Warehouse;

namespace Infrastructure.JwtToken;

public sealed record TokenClaims(
    Guid UserId,
    string Name,
    string Username,
    Role Role,
    Guid? WarehouseId,
    string? WarehouseName)
{
    public static async Task<TokenClaims> FromUserAsync(
        User user, IWarehouseRepository warehouseRepository, CancellationToken ct)
    {
        string? warehouseName = null;

        if (user.WarehouseId is { } warehouseId)
        {
            var warehouse = await warehouseRepository.GetByIdAsync(warehouseId, ct);
            warehouseName = warehouse.IsSuccess ? warehouse.Value.Name : null;
        }

        return new TokenClaims(user.Id, user.Name, user.Username, user.Role, user.WarehouseId, warehouseName);
    }
}
