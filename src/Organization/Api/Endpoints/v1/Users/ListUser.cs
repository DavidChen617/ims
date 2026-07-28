using System.Security.Claims;
using Api.Extension;
using Davish.Result;
using Domain.Users;
using Domain.Warehouse;

namespace Api.Endpoints.v1.Users;

public static class ListUserEndpoint
{
    extension(RouteGroupBuilder usersV1Group)
    {
        public RouteGroupBuilder MapListUserEndpoint()
        {
            usersV1Group.MapGet("", Handle)
                .Produces<UsersDto>()
                .WithName("ListUser")
                .WithSummary("List users")
                .WithDescription(
                    "Admin sees every user. WarehouseAdmin only sees users belonging to their own warehouse.")
                .RequireAuthorization("AdminOrWarehouseAdminOnly");

            return usersV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal caller,
        IUserRepository userRepository,
        IWarehouseRepository warehouseRepository,
        CancellationToken ct)
    {
        var isWarehouseAdmin = caller.FindFirst(ClaimTypes.Role)?.Value == nameof(Role.WarehouseAdmin);
        var warehouseId = isWarehouseAdmin ? Guid.Parse(caller.FindFirst("warehouseId")!.Value) : (Guid?)null;

        var result = await userRepository.ListAsync(warehouseId, ct)
            .ThenAsync(async users =>
            {
                var warehousesResult = await warehouseRepository.ListAsync(ct);

                return warehousesResult.Then(warehouses =>
                {
                    var warehouseNames = warehouses.ToDictionary(w => w.Id, w => w.Name);

                    return new UsersDto(users
                        .Select(u => new UserDto(
                            u.Id, u.WarehouseId,
                            u.WarehouseId is { } id && warehouseNames.TryGetValue(id, out var name) ? name : null,
                            u.Name, u.Username, u.Role, u.CreatedAt))
                        .ToList());
                });
            });

        return result.ToOk();
    }
}

public record UserDto(
    Guid Id, Guid? WarehouseId, string? WarehouseName, string Name, string Username, Role Role, DateTime CreatedAt);

public record UsersDto(IReadOnlyList<UserDto> Items);
