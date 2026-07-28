using Davish.Sendr;
using MessageContract;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Messaging;

public static class IntegrationEventSubscriptionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIntegrationEventSubscription<TEvent, THandler>()
            where TEvent : INotificationIntegrationEvent
            where THandler : class, INotificationHandler<TEvent>
        {
            services.AddNotificationHandler<TEvent>(x => x.Handler.Sequence.With<THandler>());

            services.Configure<IntegrationEventSubscriptionInfo>(
                o => o.EventTypes[typeof(TEvent).Name] = typeof(TEvent));

            return services;
        }
    }
}
