using Davish.Sendr;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.BehaviorDecorator;

public sealed class TransactionalDecorator(
    IUnitOfWork unitOfWork,
    ILogger<TransactionalDecorator> logger
) : IRequestDecorator.WithResponse
{
    public async Task<TResponse> HandleAsync<TRequest, TResponse>(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    ) where TRequest : IRequest<TResponse>
    {
        await unitOfWork.BeginAsync(cancellationToken);

        try
        {
            var result = await next();
            await unitOfWork.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception e)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(e, "An exception occurred while handling the request");
            throw;
        }
    }
}
