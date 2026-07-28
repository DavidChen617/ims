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
        CancellationToken ct)
    {
        var result = await warehouseRepository.ListAsync(ct)
            .ThenAsync(async warehouses =>
            {
                var usersResult = await userRepository.ListAsync(null, ct);

                return usersResult.Then(users => new WarehousesDto(warehouses
                    .Select(w => new WarehouseListItemDto(
                        w.Id,
                        w.Name,
                        users.FirstOrDefault(u => u.WarehouseId == w.Id && u.Role == Role.WarehouseAdmin)?.Name,
                        users.Count(u => u.WarehouseId == w.Id && u.Role == Role.WarehouseUser)))
                    .ToList()));
            });

        return result.ToOk();
    }
}

public record WarehouseListItemDto(Guid Id, string Name, string? WarehouseAdminName, int StaffCount);

public record WarehousesDto(IReadOnlyList<WarehouseListItemDto> Items);
