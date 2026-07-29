using System.Security.Claims;
using Api.Extension;
using Davish.Result;
using Domain.Users;
using Domain.Warehouse;

namespace Api.Endpoints.v1.Users;

public static class ListWarehouseUserEndpoint
{
    extension(RouteGroupBuilder usersV1Group)
    {
        public RouteGroupBuilder MapListWarehouseUserEndpoint()
        {
            usersV1Group.MapGet("warehouse", Handle)
                .Produces<UsersDto>()
                .WithName("ListWarehouseUser")
                .WithSummary("List users in the caller's own warehouse")
                .WithDescription(
                    "WarehouseAdmin and WarehouseUser both see the staff belonging to their own warehouse — " +
                    "e.g. to populate an applicant/reviewer filter by name while still submitting a user id.")
                .RequireAuthorization("WarehouseStaffOnly");

            return usersV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal caller,
        IUserRepository userRepository,
        IWarehouseRepository warehouseRepository,
        CancellationToken ct)
    {
        var warehouseId = Guid.Parse(caller.FindFirst("warehouseId")!.Value);

        var result = await userRepository.ListAsync(warehouseId, ct)
            .ThenAsync(async users =>
            {
                var warehouseResult = await warehouseRepository.GetByIdAsync(warehouseId, ct);

                return warehouseResult.Then(warehouse => new UsersDto(users
                    .Select(u => new UserDto(u.Id, u.WarehouseId, warehouse.Name, u.Name, u.Username, u.Role, u.CreatedAt))
                    .ToList()));
            });

        return result.ToOk();
    }
}

public record UsersDto(IReadOnlyList<UserDto> Items);
