using Domain.Users;
using Domain.Warehouse;

namespace Infrastructure.JwtToken;

// 把 token 產生跟 User 聚合根解耦 —— WarehouseName 不是 User 自己擁有的資料
// (它是呼叫端另外查 Warehouse 才解析出來的),不應該只為了滿足這一個需求
// 就把它塞進 entity 本身。
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
