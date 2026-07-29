using Dapper;
using Infrastructure.Outbox;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.IntegrationTest;

public class OutboxStoreTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GivenDeadLetteredMessage_WhenListed_ThenIsExcluded()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrderingUnitOfWork>();
        var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var pending = OutboxMessage.Create(Guid.CreateVersion7(), "SomeEvent", "{}");
        var deadLettered = OutboxMessage.Create(Guid.CreateVersion7(), "SomeEvent", "{}");
        deadLettered.SetError("permanent failure");
        deadLettered.MarkDeadLettered();

        try
        {
            await outboxStore.AddAsync(pending, CancellationToken.None);
            await outboxStore.AddAsync(deadLettered, CancellationToken.None);
            await outboxStore.SaveAsync(deadLettered, CancellationToken.None);

            var listed = await outboxStore.ListAsync(50, CancellationToken.None);

            Assert.Contains(listed, m => m.Id == pending.Id);
            Assert.DoesNotContain(listed, m => m.Id == deadLettered.Id);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync(
                "delete from outbox_messages where id in (@PendingId, @DeadLetteredId)",
                new { PendingId = pending.Id, DeadLetteredId = deadLettered.Id });
        }
    }
}
