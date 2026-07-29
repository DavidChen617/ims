using Infrastructure.Outbox;

namespace Ordering.UnitTest;

public class OutboxMessageTests
{
    [Fact]
    public void GivenNewMessage_WhenSetErrorCalled_ThenRetryCountIncrements()
    {
        var message = OutboxMessage.Create(Guid.CreateVersion7(), "SomeEvent", "{}");

        message.SetError("boom");
        Assert.Equal(1, message.RetryCount);
        Assert.Equal("boom", message.Error);

        message.SetError("boom again");
        Assert.Equal(2, message.RetryCount);
        Assert.Equal("boom again", message.Error);
    }

    [Fact]
    public void GivenMessage_WhenMarkDeadLettered_ThenDeadLetteredAtIsSet()
    {
        var message = OutboxMessage.Create(Guid.CreateVersion7(), "SomeEvent", "{}");

        Assert.Null(message.DeadLetteredAt);

        message.MarkDeadLettered();

        Assert.NotNull(message.DeadLetteredAt);
    }

    [Fact]
    public void GivenNewMessage_WhenProcessed_ThenNotDeadLettered()
    {
        var message = OutboxMessage.Create(Guid.CreateVersion7(), "SomeEvent", "{}");

        message.Process();

        Assert.NotNull(message.ProcessedOn);
        Assert.Null(message.DeadLetteredAt);
    }
}
