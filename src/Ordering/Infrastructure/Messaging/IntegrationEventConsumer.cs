using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Davish.Sendr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            consumer.Subscribe(topicOptions.Value.Inventory);

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
                        // ProcessMessageAsync 自己不會拋例外(跳過/成功/dead-letter 全部都在
                        // 內部接住了)—— 只有關閉時觸發的取消會逃出來。
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
            if (JsonSerializer.Deserialize(result.Message.Value, eventType) is not INotification notification)
                return;

            using var scope = factory.CreateScope();
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

        await producer.ProduceAsync(topicOptions.Value.InventoryDeadLetter, deadLetter, ct);
    }
}
