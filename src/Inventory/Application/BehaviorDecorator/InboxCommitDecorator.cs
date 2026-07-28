using Davish.Sendr;
using MessageContract;
using SharedKernel;

namespace Application.BehaviorDecorator;

// 讓每個 integration event handler 不用自己在最後手動呼叫
// inbox.MarkProcessedAsync + unitOfWork.CommitAsync —— 統一交給這層做。
// handler 只要負責跑完自己的業務邏輯,結束時保證有一個開著的 transaction 就好
// (正常路徑是 IntegrationEventConsumer 一開始開的那個;insufficient-stock 那種
// rollback 後重開一個新 transaction 的分支,也只要開好新 transaction、不用自己 commit)。
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
