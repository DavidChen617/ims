using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Ordering.IntegrationTest.Messaging;

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
