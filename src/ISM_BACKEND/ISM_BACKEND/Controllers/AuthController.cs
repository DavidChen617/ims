using ISM_BACKEND.Models;
using ISM_BACKEND.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM_BACKEND.Controllers;

[Route("api/auth")]
[Authorize]
public class AuthController : BaseController
{
    private readonly AuthService _svc;

    public AuthController(AuthService svc) => _svc = svc;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Login([FromBody] LoginRequest body)
    {
        var result = await _svc.LoginAsync(body.username, body.password);
        return result != null
            ? Ok(ApiResponse<TokenResponse>.Ok(result, "登入成功"))
            : Unauthorized(ApiResponse<TokenResponse>.Fail("帳號或密碼錯誤"));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequest body)
        => await _svc.LogoutAsync(body.refreshToken)
            ? Ok(ApiResponse<object>.Ok(new { }, "已登出"))
            : Unauthorized(ApiResponse<object>.Fail("RefreshToken 無效或已過期"));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Refresh([FromBody] RefreshRequest body)
    {
        var result = await _svc.RefreshAsync(body.userId, body.refreshToken);
        return result != null
            ? Ok(ApiResponse<TokenResponse>.Ok(result, "換發成功"))
            : Unauthorized(ApiResponse<TokenResponse>.Fail("RefreshToken 無效或已過期"));
    }

    [HttpPost("admin/register-user")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> AdminRegisterUser([FromBody] RegisterUserRequest body)
    {
        if (body.password.Length < 8)
            throw new ArgumentException("密碼長度至少 8 碼");

        var result = await _svc.RegisterUserAsync(body.warehouseId, body.name, body.username, body.password, body.role);
        return Ok(ApiResponse<TokenResponse>.Ok(result, "使用者建立成功"));
    }

    [HttpPost("warehouse-admin/register-user")]
    [Authorize(Roles = "WarehouseAdmin")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> WarehouseAdminRegisterUser([FromBody] RegisterWarehouseUserRequest body)
    {
        if (body.password.Length < 8)
            throw new ArgumentException("密碼長度至少 8 碼");

        var warehouseId = GetCurrentWarehouseId()!.Value;
        var result = await _svc.RegisterWarehouseUserAsync(warehouseId, body.name, body.username, body.password);
        return Ok(ApiResponse<TokenResponse>.Ok(result, "使用者建立成功"));
    }
}
