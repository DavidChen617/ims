using Davish.Sendr;
using MessageContract;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Messaging;

public static class IntegrationEventSubscriptionExtensions
{
    extension(IServiceCollection services)
    {
        // configure 讓每個呼叫端自己決定要不要裝飾器 —— 這支是給任何 integration event
        // 訂閱共用的基礎設施,不該替所有呼叫端綁死同一種行為。
        public IServiceCollection AddIntegrationEventSubscription<TEvent, THandler>(
            Action<NotificationHandlerEntryOptions<TEvent>>? configure = null)
            where TEvent : INotificationIntegrationEvent
            where THandler : class, INotificationHandler<TEvent>
        {
            services.AddNotificationHandler<TEvent>(x => x.Handler.Sequence.With<THandler>(configure));

            services.Configure<IntegrationEventSubscriptionInfo>(
                o => o.EventTypes[typeof(TEvent).Name] = typeof(TEvent));

            return services;
        }
    }
}
