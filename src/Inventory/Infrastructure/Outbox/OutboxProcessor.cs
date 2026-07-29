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
    private const int MaxPollRetries = 5;
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
                    $"{topicOptions.Value.Inventory} publish", ActivityKind.Producer);
                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination.name", topicOptions.Value.Inventory);
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
                        async ct => await producer.ProduceAsync(topicOptions.Value.Inventory, kafkaMessage, ct),
                        stoppingToken);
                    message.Process();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    message.SetError(ex.Message);

                    // Polly 那 3 次重試是每一次 poll 內的重試;這裡的 MaxPollRetries 是跨 poll
                    // cycle 累計的次數(每 5 秒一次)。超過門檻才真的放棄,發到 DLQ topic 讓人
                    // 事後可以查、可以重放 —— 不是無限重試,也不是悄悄標記成功。
                    if (message.RetryCount >= MaxPollRetries)
                    {
                        try
                        {
                            await PublishToDeadLetterAsync(message, ex, stoppingToken);
                            message.MarkDeadLettered();
                        }
                        catch (Exception dlqEx) when (dlqEx is not OperationCanceledException)
                        {
                            // DLQ 本身也發不出去(通常代表整個 broker 都連不上,不是這筆訊息
                            // 特有的問題)—— 不標記 DeadLetteredAt,讓它下一輪繼續重試整套流程,
                            // 而不是讓這個例外往外炸、把整個 BackgroundService/host 弄掛。
                            activity?.SetStatus(ActivityStatusCode.Error, dlqEx.Message);
                        }
                    }
                }

                await outboxStore.SaveAsync(message, stoppingToken);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task PublishToDeadLetterAsync(OutboxMessage message, Exception ex, CancellationToken ct)
    {
        var deadLetter = new Message<string, string>
        {
            Key = message.Id.ToString(),
            Value = message.Payload,
            Headers = new Headers
            {
                { "EventType", Encoding.UTF8.GetBytes(message.EventType) },
                { "Error", Encoding.UTF8.GetBytes(ex.Message) },
                { "FailedAt", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
            }
        };

        await producer.ProduceAsync(topicOptions.Value.InventoryDeadLetter, deadLetter, ct);
    }
}
