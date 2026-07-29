using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace Inventory.IntegrationTest.Messaging;

public class KafkaCustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string OrderingTopic = "ordering.events";
    public const string OrderingDeadLetterTopic = "ordering.events.dlq";
    public const string InventoryTopic = "inventory.events";

    public string BootstrapServers => _kafka.GetBootstrapAddress();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("inventory_db_test")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.5.12").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Kafka__BootstrapServers", _kafka.GetBootstrapAddress());

        await MigrationRunner.ApplyAsync(_postgres.GetConnectionString());
        await CreateTopicsAsync();

        using var warmup = Services.CreateScope();
    }

    private async Task CreateTopicsAsync()
    {
        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _kafka.GetBootstrapAddress() }).Build();

        await adminClient.CreateTopicsAsync([
            new TopicSpecification { Name = OrderingTopic, NumPartitions = 1, ReplicationFactor = 1 },
            new TopicSpecification { Name = OrderingDeadLetterTopic, NumPartitions = 1, ReplicationFactor = 1 },
            new TopicSpecification { Name = InventoryTopic, NumPartitions = 1, ReplicationFactor = 1 }
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
