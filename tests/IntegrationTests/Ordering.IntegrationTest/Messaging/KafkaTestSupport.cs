using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Ordering.IntegrationTest.Messaging;

// 測試端用來代替 Ordering 以外「真正的」Kafka producer/consumer —— 用來塞訊息讓
// app 自己的 IntegrationEventConsumer 去消費,以及觀察 app 自己的 OutboxProcessor
// 應該送出的訊息。
internal static class KafkaTestSupport
{
    public static IProducer<string, string> CreateProducer(string bootstrapServers) =>
        new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = bootstrapServers }).Build();

    public static IConsumer<string, string> CreateConsumer(string bootstrapServers, string topic, string groupId)
    {
        var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(topic);
        return consumer;
    }

    public static async Task ProduceAsync(IProducer<string, string> producer, string topic, string eventType, object @event)
    {
        var message = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = JsonSerializer.Serialize(@event, @event.GetType()),
            Headers = new Headers { { "EventType", Encoding.UTF8.GetBytes(eventType) } }
        };

        await producer.ProduceAsync(topic, message);
    }

    // 從 `consumer` 讀取(並丟棄)訊息,直到出現一筆 EventType header 對得上、且反序列化後的
    // payload 滿足 `predicate` 的訊息,或是 `timeout` 到了為止。其他測試共用同一個 topic
    // 產生的訊息單純跳過,不當成失敗。
    public static T? ConsumeMatching<T>(
        IConsumer<string, string> consumer, string expectedEventType, Func<T, bool> predicate, TimeSpan timeout)
        where T : class
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(300));
            if (result?.Message is null)
                continue;

            if (!result.Message.Headers.TryGetLastBytes("EventType", out var eventTypeBytes)
                || Encoding.UTF8.GetString(eventTypeBytes) != expectedEventType)
                continue;

            var candidate = JsonSerializer.Deserialize<T>(result.Message.Value);
            if (candidate is not null && predicate(candidate))
                return candidate;
        }

        return null;
    }

    // 概念一樣,但用於原始訊息(DLQ 情境的 payload 本來就不是合法格式,沒辦法反序列化)。
    public static ConsumeResult<string, string>? ConsumeRawMatching(
        IConsumer<string, string> consumer, Func<ConsumeResult<string, string>, bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(300));
            if (result?.Message is not null && predicate(result))
                return result;
        }

        return null;
    }
}
