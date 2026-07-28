using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SharedKernel.Telemetry;

namespace Infrastructure.Outbox;

public sealed class OutboxProcessor(
    IServiceScopeFactory factory,
    IProducer<string, string> producer,
    IOptions<KafkaTopicOptions> topicOptions
) : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = factory.CreateScope();
            var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

            var messages = await outboxStore.ListAsync(BatchSize, stoppingToken);

            foreach (var message in messages)
            {
                using var activity = MessagingActivitySource.Instance.StartActivity(
                    $"{topicOptions.Value.Ordering} publish", ActivityKind.Producer);
                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination.name", topicOptions.Value.Ordering);
                activity?.SetTag("messaging.message.type", message.EventType);

                try
                {
                    var headers = new Headers { { "EventType", Encoding.UTF8.GetBytes(message.EventType) } };
                    if (activity?.Id is { } traceParent)
                        headers.Add("traceparent", Encoding.UTF8.GetBytes(traceParent));

                    var kafkaMessage = new Message<string, string>
                    {
                        Key = message.Id.ToString(),
                        Value = message.Payload,
                        Headers = headers
                    };

                    await KafkaRetryPipeline.Produce.ExecuteAsync(
                        async ct => await producer.ProduceAsync(topicOptions.Value.Ordering, kafkaMessage, ct),
                        stoppingToken);
                    message.Process();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    message.SetError(ex.Message);
                    // Polly 那 3 次重試都失敗就直接放棄,標記已處理,不然 ListAsync 會每個
                    // poll cycle 一直撈到同一筆永久失敗的訊息,無限重試下去。
                    message.Process();
                }

                await outboxStore.SaveAsync(message, stoppingToken);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }
}
