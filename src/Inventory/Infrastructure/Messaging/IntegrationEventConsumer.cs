using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Application;
using Confluent.Kafka;
using Davish.Sendr;
using Infrastructure.Inbox;
using MessageContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Telemetry;

namespace Infrastructure.Messaging;

public sealed class IntegrationEventConsumer(
    IServiceScopeFactory factory,
    IConsumer<string, string> consumer,
    IProducer<string, string> producer,
    IOptions<IntegrationEventSubscriptionInfo> subscriptionOptions,
    IOptions<KafkaTopicOptions> topicOptions,
    ILogger<IntegrationEventConsumer> logger
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(async () =>
        {
            consumer.Subscribe(topicOptions.Value.Ordering);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    ConsumeResult<string, string> result;

                    try
                    {
                        result = consumer.Consume(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ConsumeException)
                    {
                        continue;
                    }

                    try
                    {
                        // ProcessMessageAsync 自己不會拋例外(跳過/成功/dead-letter 全部都在內部接住了)—— 只有關閉時觸發的取消會逃出來。
                        await ProcessMessageAsync(result, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    try
                    {
                        consumer.Commit(result);
                    }
                    catch (KafkaException ex)
                    {
                        logger.LogWarning(ex, "Failed to commit offset for {Topic}/{Partition}/{Offset}",
                            result.Topic, result.Partition.Value, result.Offset.Value);
                    }
                }
            }
            finally
            {
                consumer.Close();
            }
        }, stoppingToken);

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        if (!result.Message.Headers.TryGetLastBytes("EventType", out var eventTypeBytes))
            return;

        if (!subscriptionOptions.Value.EventTypes.TryGetValue(
                Encoding.UTF8.GetString(eventTypeBytes), out var eventType))
            return;

        var parentContext = ExtractParentContext(result);
        using var activity = MessagingActivitySource.Instance.StartActivity(
            $"{result.Topic} process", ActivityKind.Consumer, parentContext);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.source.name", result.Topic);
        activity?.SetTag("messaging.message.type", Encoding.UTF8.GetString(eventTypeBytes));

        try
        {
            if (JsonSerializer.Deserialize(result.Message.Value, eventType) is not INotificationIntegrationEvent notification)
                return;

            using var scope = factory.CreateScope();

            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();

            // 這裡統一幫每一種消費的事件類型做一次去重 —— handler 不用再自己呼叫這個了。
            // 但 MarkProcessedAsync/CommitAsync(還有庫存不足那條「Rollback 後在全新
            // transaction 重試」的分支)還是各 handler 自己負責,因為那部分真的是 handler 各自特有的邏輯。
            if (!await InboxGuard.BeginIfNotProcessedAsync(unitOfWork, inbox, notification.Id, ct))
                return;

            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(notification, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await PublishToDeadLetterAsync(result, ex, ct);
        }
    }

    private static ActivityContext ExtractParentContext(ConsumeResult<string, string> result)
    {
        if (result.Message.Headers.TryGetLastBytes("traceparent", out var traceParentBytes)
            && ActivityContext.TryParse(Encoding.UTF8.GetString(traceParentBytes), null, out var parentContext))
            return parentContext;

        return default;
    }

    private async Task PublishToDeadLetterAsync(ConsumeResult<string, string> result, Exception ex, CancellationToken ct)
    {
        var deadLetter = new Message<string, string>
        {
            Key = result.Message.Key,
            Value = result.Message.Value,
            Headers = new Headers
            {
                { "EventType", result.Message.Headers.FirstOrDefault(h => h.Key == "EventType")?.GetValueBytes() ?? [] },
                { "Error", Encoding.UTF8.GetBytes(ex.Message) },
                { "FailedAt", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
            }
        };

        await producer.ProduceAsync(topicOptions.Value.OrderingDeadLetter, deadLetter, ct);
    }
}
