using Confluent.Kafka;
using Polly;
using Polly.Retry;

namespace Infrastructure.Messaging;

public static class KafkaRetryPipeline
{
    public static readonly ResiliencePipeline Produce = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<ProduceException<string, string>>(),
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(1)
        })
        .Build();
}
