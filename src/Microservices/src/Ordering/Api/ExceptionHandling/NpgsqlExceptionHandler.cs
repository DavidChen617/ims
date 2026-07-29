using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Api.ExceptionHandling;

public sealed class NpgsqlExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<NpgsqlExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not PostgresException postgresException)
            return false;

        var (statusCode, title) = postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => (StatusCodes.Status409Conflict, "資料已存在,違反唯一性限制"),
            PostgresErrorCodes.ForeignKeyViolation => (StatusCodes.Status400BadRequest, "關聯的資料不存在,或仍被其他資料參照"),
            PostgresErrorCodes.NotNullViolation => (StatusCodes.Status400BadRequest, "缺少必填欄位"),
            _ => (StatusCodes.Status500InternalServerError, "資料庫發生未預期的錯誤")
        };

        logger.LogError(exception, "未處理的 PostgresException,SqlState={SqlState}", postgresException.SqlState);

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Title = title, Status = statusCode }
        });
    }
}
