using ISM_BACKEND.Models;
using ISM_BACKEND.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM_BACKEND.Controllers;

[Route("api/users")]
[Authorize(Roles = "Admin,WarehouseAdmin")]
public class UsersController : BaseController
{
    private readonly UserService _svc;

    public UsersController(UserService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserListItem>>>> List(
        [FromQuery] string? name,
        [FromQuery] string? username,
        [FromQuery] string? role,
        [FromQuery] long? warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // WarehouseAdmin 強制只能看自己倉庫,不管 query string 傳了什麼
        if (GetRequiredRole() == "WarehouseAdmin")
            warehouseId = GetCurrentWarehouseId();

        var data = await _svc.ListUsersAsync(name, username, role, warehouseId, page, pageSize);
        return Ok(ApiResponse<PagedResult<UserListItem>>.Ok(data));
    }
}
