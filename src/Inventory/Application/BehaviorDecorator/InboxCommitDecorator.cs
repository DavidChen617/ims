using Davish.Sendr;
using MessageContract;
using SharedKernel;

namespace Application.BehaviorDecorator;

public sealed class InboxCommitDecorator(IInboxStore inbox, IUnitOfWork unitOfWork) : INotificationDecorator
{
    public async Task HandleAsync<TNotification>(
        TNotification notification,
        NotificationHandlerDelegate next,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        await next();

        if (notification is IIntegrationEvent integrationEvent)
        {
            await inbox.MarkProcessedAsync(integrationEvent.Id, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
