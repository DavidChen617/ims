using System.Net;
using System.Text.Json;
using ISM_BACKEND.Models;

namespace ISM_BACKEND.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "輸入驗證失敗");
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未預期的例外");
            await WriteResponseAsync(context, HttpStatusCode.InternalServerError, "系統發生錯誤,請稍後再試");
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        var response = ApiResponse<object>.Fail(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
