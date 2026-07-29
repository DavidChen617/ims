using Confluent.Kafka;
using Polly;
using Polly.Retry;

namespace Infrastructure.Messaging;

public static class KafkaRetryPipeline
{
    // ProduceAsync 只會拋出 ProduceException<TKey,TValue>(或是呼叫端寫錯時的
    // ArgumentException);重試的目標就精準對應到 client 文件說明的那些
    // broker/傳輸層失敗情況。
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
