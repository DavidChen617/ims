using Davish.Sendr;

namespace MessageContract;

public interface IIntegrationEventHandler<in TIntegrationEvent>
    : INotificationHandler<TIntegrationEvent> 
    where TIntegrationEvent : INotificationIntegrationEvent;
