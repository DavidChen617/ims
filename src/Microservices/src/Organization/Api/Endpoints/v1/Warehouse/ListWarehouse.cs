using Api.Extension;
using Davish.Result;
using Domain.Users;
using Domain.Warehouse;

namespace Api.Endpoints.v1.Warehouse;

public static class ListWarehouseEndpoint
{
    extension(RouteGroupBuilder warehouseV1Group)
    {
        public RouteGroupBuilder MapListWarehouseEndpoint()
        {
            warehouseV1Group.MapGet("", Handle)
                .Produces<WarehousesDto>()
                .WithName("ListWarehouse")
                .WithSummary("List all warehouses")
                .WithDescription(
                    "Return all warehouses, each with its warehouse admin's name (if assigned) and regular staff count.")
                .RequireAuthorization("AdminOnly");

            return warehouseV1Group;
        }
    }

    private static async Task<IResult> Handle(
        IWarehouseRepository warehouseRepository,
        IUserRepository userRepository,
        CancellationToken ct,
        [AsParameters] ListWarehouseRequest request)
    {
        var result = await warehouseRepository.ListAsync(request.Name, ct)
            .ThenAsync(async warehouses =>
            {
                var usersResult = await userRepository.ListAsync(null, ct);

                return usersResult.Then(users =>
                {
                    // WarehouseAdminName/StaffCount 不是資料表欄位,是 join 完之後才算出來的,
                    // 所以這兩個篩選只能在算完之後、拿到記憶體裡的清單再做,SQL 層面辦不到。
                    var items = warehouses
                        .Select(w => new WarehouseListItemDto(
                            w.Id,
                            w.Name,
                            users.FirstOrDefault(u => u.WarehouseId == w.Id && u.Role == Role.WarehouseAdmin)?.Name,
                            users.Count(u => u.WarehouseId == w.Id && u.Role == Role.WarehouseUser)))
                        .Where(item => request.WarehouseAdminName is null
                            || (item.WarehouseAdminName?.Contains(request.WarehouseAdminName, StringComparison.OrdinalIgnoreCase) ?? false))
                        .Where(item => request.StaffCountMin is null || item.StaffCount >= request.StaffCountMin)
                        .Where(item => request.StaffCountMax is null || item.StaffCount <= request.StaffCountMax)
                        .ToList();

                    return new WarehousesDto(items);
                });
            });

        return result.ToOk();
    }
}

public sealed record ListWarehouseRequest(
    string? Name = null,
    string? WarehouseAdminName = null,
    int? StaffCountMin = null,
    int? StaffCountMax = null);

public record WarehouseListItemDto(Guid Id, string Name, string? WarehouseAdminName, int StaffCount);

public record WarehousesDto(IReadOnlyList<WarehouseListItemDto> Items);
