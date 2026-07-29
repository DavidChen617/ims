using Davish.Sendr;

namespace SharedKernel;

public interface IDomainEvent: INotification
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}

public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent> where TDomainEvent : IDomainEvent;
