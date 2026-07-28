using Application.BehaviorDecorator;
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
            // InboxCommitDecorator 包在每個 handler 外面,處理完就自動
            // MarkProcessedAsync + CommitAsync,handler 自己不用再管這件事。
            services.AddNotificationHandler<TEvent>(
                x => x.Handler.Sequence.With<THandler>(h => h.Decorator.With<InboxCommitDecorator>()));

            services.Configure<IntegrationEventSubscriptionInfo>(
                o => o.EventTypes[typeof(TEvent).Name] = typeof(TEvent));

            return services;
        }
    }
}
