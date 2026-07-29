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
                .Produces<PagedUsersDto>()
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
        CancellationToken ct,
        [AsParameters] ListUserRequest request)
    {
        var isWarehouseAdmin = caller.FindFirst(ClaimTypes.Role)?.Value == nameof(Role.WarehouseAdmin);
        var warehouseId = isWarehouseAdmin ? Guid.Parse(caller.FindFirst("warehouseId")!.Value) : (Guid?)null;
        var page = request.Page ?? 1;
        var size = request.Size ?? 20;

        var usersResult = await userRepository.ListPagedAsync(
            warehouseId, request.Name, request.Username, request.Role, request.WarehouseName,
            request.CreatedFrom, request.CreatedTo, page, size, ct);
        if (!usersResult.IsSuccess)
            return usersResult.ToProblemDetails();

        var countResult = await userRepository.CountAsync(
            warehouseId, request.Name, request.Username, request.Role, request.WarehouseName,
            request.CreatedFrom, request.CreatedTo, ct);
        if (!countResult.IsSuccess)
            return countResult.ToProblemDetails();

        var warehousesResult = await warehouseRepository.ListAsync(ct);
        if (!warehousesResult.IsSuccess)
            return warehousesResult.ToProblemDetails();

        var warehouseNames = warehousesResult.Value.ToDictionary(w => w.Id, w => w.Name);

        var items = usersResult.Value
            .Select(u => new UserDto(
                u.Id, u.WarehouseId,
                u.WarehouseId is { } id && warehouseNames.TryGetValue(id, out var name) ? name : null,
                u.Name, u.Username, u.Role, u.CreatedAt))
            .ToList();

        return TypedResults.Ok(new PagedUsersDto(items, countResult.Value, page, size));
    }
}

public sealed record ListUserRequest(
    string? Name = null,
    string? Username = null,
    Role? Role = null,
    string? WarehouseName = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    int? Page = null,
    int? Size = null);

public record UserDto(
    Guid Id, Guid? WarehouseId, string? WarehouseName, string Name, string Username, Role Role, DateTime CreatedAt);

public record PagedUsersDto(IReadOnlyList<UserDto> Items, int TotalCount, int Page, int Size);
