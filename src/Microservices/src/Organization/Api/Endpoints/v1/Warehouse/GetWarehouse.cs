using Api.Extension;
using Davish.Result;
using Domain.Users;
using Domain.Warehouse;

namespace Api.Endpoints.v1.Warehouse;

public static class GetWarehouseEndpoint
{
    extension(RouteGroupBuilder warehouseV1Group)
    {
        public RouteGroupBuilder MapGetWarehouseEndpoint()
        {
            warehouseV1Group.MapGet("{id:guid}", Handle)
                .Produces<WarehouseDetailDto>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithName("GetWarehouse")
                .WithSummary("Get a warehouse's detail")
                .WithDescription("Get a warehouse by id, including its warehouse admins and regular users.")
                .RequireAuthorization("AdminOnly");

            return warehouseV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        IWarehouseRepository warehouseRepository,
        IUserRepository userRepository,
        CancellationToken ct)
    {
        var result = await warehouseRepository.GetByIdAsync(id, ct)
            .ThenAsync(async warehouse =>
            {
                var usersResult = await userRepository.ListAsync(warehouse.Id, ct);
                if (!usersResult.IsSuccess)
                    return Result.Failure<WarehouseDetailDto>(usersResult.Error);

                return Result.Success(new WarehouseDetailDto(
                    warehouse.Id,
                    warehouse.Name,
                    usersResult.Value.Where(u => u.Role == Role.WarehouseAdmin)
                        .Select(u => new WarehouseStaffDto(u.Id, u.Name)).ToList(),
                    usersResult.Value.Where(u => u.Role == Role.WarehouseUser)
                        .Select(u => new WarehouseStaffDto(u.Id, u.Name)).ToList()));
            });

        return result.ToOk();
    }
}

public record WarehouseStaffDto(Guid Id, string Name);

public record WarehouseDetailDto(
    Guid Id,
    string Name,
    IReadOnlyList<WarehouseStaffDto> WarehouseAdmins,
    IReadOnlyList<WarehouseStaffDto> WarehouseUsers);
