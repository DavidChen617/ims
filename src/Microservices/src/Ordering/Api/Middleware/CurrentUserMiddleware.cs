using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Identity;
using Application;

using Application.Abstracts;
namespace Api.Middleware;

public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var currentUser = (CurrentUser)context.RequestServices.GetRequiredService<ICurrentUser>();

            currentUser.UserId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            currentUser.Name = context.User.FindFirstValue(JwtRegisteredClaimNames.Name)!;
            currentUser.Username = context.User.FindFirstValue(ClaimTypes.Name)!;
            currentUser.Role = context.User.FindFirstValue(ClaimTypes.Role)!;
            
            if (Guid.TryParse(context.User.FindFirstValue("warehouseId"), out var warehouseId))
                currentUser.WarehouseId = warehouseId;

            currentUser.WarehouseName = context.User.FindFirstValue("warehouseName");
        }

        await next(context);
    }
}
