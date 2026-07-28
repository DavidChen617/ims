using Davish.Sendr;

namespace MessageContract;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}

public interface INotificationIntegrationEvent : IIntegrationEvent, INotification;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
