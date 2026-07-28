using Application;
using SharedKernel;

namespace Infrastructure.Inbox;

public static class InboxGuard
{
    // 開一個 transaction,並在裡面檢查 inbox。如果這個事件已經處理過,回傳 false(並 rollback)
    // —— 這種情況呼叫端什麼都不用做。回傳 true 的話,呼叫端就自己跑業務邏輯,並且要負責在
    // 結束前自己呼叫 IInboxStore.MarkProcessedAsync 跟 IUnitOfWork.CommitAsync
    // (可能不只一條 commit 路徑,例如 rollback 後在全新 transaction 裡重試的分支)。
    public static async Task<bool> BeginIfNotProcessedAsync(
        IUnitOfWork unitOfWork, IInboxStore inbox, Guid eventId, CancellationToken ct)
    {
        await unitOfWork.BeginAsync(ct);

        if (!await inbox.HasProcessedAsync(eventId, ct))
            return true;

        await unitOfWork.RollbackAsync(ct);
        return false;
    }
}
