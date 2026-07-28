using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace Ordering.IntegrationTest.Messaging;

// 跟 CustomWebApplicationFactory 不同,這個刻意保留 OutboxProcessor 跟
// IntegrationEventConsumer 讓它們跑起來 —— 這正是這組測試存在的意義:驗證真正的
// produce/consume 路徑對著一個真實(用完就丟)的 broker,而不只是 repository/DB 的行為。
public class KafkaCustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string OrderingTopic = "ordering.events";
    public const string InventoryTopic = "inventory.events";
    public const string InventoryDeadLetterTopic = "inventory.events.dlq";

    public string BootstrapServers => _kafka.GetBootstrapAddress();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("ordering_db_test")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    // confluentinc/cp-kafka 目前實際最新版在這個測試套件的版本下開不起來 ——
    // Testcontainers.Kafka 4.13.0 的 vendor/entrypoint 偵測邏輯不認識 cp-kafka 8.x 的 image
    // 佈局。7.5.12 是套件自己內建的預設值 —— 這裡明確指定,因為無參數的 KafkaBuilder() 建構子
    // 已經過時,但版本選的是這個測試套件驗證過能用的版本,而不是追著 broker 自己的最新版跑。
    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.5.12").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

        // Program.cs 會很早就讀取這些設定,早於 ConfigureWebHost hook 生效的時機 ——
        // 必須在 host 建置前就設好,道理跟 CustomWebApplicationFactory 一樣。
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Kafka__BootstrapServers", _kafka.GetBootstrapAddress());

        await MigrationRunner.ApplyAsync(_postgres.GetConnectionString());
        await CreateTopicsAsync();

        // WebApplicationFactory 是延遲建置的,第一次存取 Services/Server/CreateClient() 才會
        // 真的把 host 建起來 —— 這裡先強制建置一次,確保不管測試方法的執行順序如何,
        // OutboxProcessor/IntegrationEventConsumer 在任何測試方法發訊息之前就已經在跑了。
        using var warmup = Services.CreateScope();
    }

    private async Task CreateTopicsAsync()
    {
        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _kafka.GetBootstrapAddress() }).Build();

        await adminClient.CreateTopicsAsync([
            new TopicSpecification { Name = OrderingTopic, NumPartitions = 1, ReplicationFactor = 1 },
            new TopicSpecification { Name = InventoryTopic, NumPartitions = 1, ReplicationFactor = 1 },
            new TopicSpecification { Name = InventoryDeadLetterTopic, NumPartitions = 1, ReplicationFactor = 1 }
        ]);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _kafka.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
    }
}
