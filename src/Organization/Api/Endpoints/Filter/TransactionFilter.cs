using Infrastructure.Persistence;

namespace Api.Endpoints.Filter;

public sealed class TransactionFilter(ILogger<TransactionFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var ct = context.HttpContext.RequestAborted;
        var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IOrganizationUnitOfWork>();
        await unitOfWork.BeginAsync(ct);
        
        try
        {
            var result = await next(context);
            await unitOfWork.CommitAsync(ct);
            return result;
        }
        catch (Exception e)
        {
            await unitOfWork.RollbackAsync(ct);

            logger.LogError("Error while executing TransactionFilter: {Error}",
                e.InnerException is not null ? e.InnerException.Message : e.Message);

            throw;
        }
    }
}
