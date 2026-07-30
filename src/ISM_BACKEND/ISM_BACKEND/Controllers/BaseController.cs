using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ISM_BACKEND.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected long GetRequiredUserId()
        => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected string GetRequiredUserName()
        => User.FindFirstValue(ClaimTypes.Name) ?? "";

    protected string GetRequiredRole()
        => User.FindFirstValue(ClaimTypes.Role) ?? "";

    protected long? GetCurrentWarehouseId()
    {
        var raw = User.FindFirstValue("warehouseId");
        return string.IsNullOrEmpty(raw) ? null : long.Parse(raw);
    }
}
