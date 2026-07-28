using Application;
using Application.BehaviorDecorator;
using Application.Stocks;
using Application.Stocks.EventHandling;
using Confluent.Kafka;
using Dapper;
using Domain.Stocks;
using Infrastructure.Messaging;
using Infrastructure.Outbox;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Readers;
using Infrastructure.Persistence.Repositories;
using MessageContract.InboundOrders;
using MessageContract.OutboundOrders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

using Application.Abstracts;
namespace Infrastructure;

public static class Dependency
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            services
                .AddScoped<IInventoryUnitOfWork, InventoryUnitOfWork>()
                .AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IInventoryUnitOfWork>())
                .AddScoped<IStockRepository, StockRepository>()
                .AddScoped<IStockReader, StockReader>()
                .AddScoped<IInboxStore, InboxStore>()
                .AddScoped<IOutboxStore, OutboxStore>()
                .AddScoped<IIntegrationEventWriter, IntegrationEventWriter>()
                .Configure<KafkaTopicOptions>(o => configuration.GetSection("Kafka:Topics").Bind(o))
                .AddSingleton<IProducer<string, string>>(_ =>
                {
                    var config = new ProducerConfig { BootstrapServers = configuration["Kafka:BootstrapServers"] };

                    return new ProducerBuilder<string, string>(config).Build();
                })
                .AddSingleton<IConsumer<string, string>>(_ =>
                {
                    var config = new ConsumerConfig
                    {
                        BootstrapServers = configuration["Kafka:BootstrapServers"],
                        GroupId = "inventory-service",
                        AutoOffsetReset = AutoOffsetReset.Earliest,
                        EnableAutoCommit = false
                    };

                    return new ConsumerBuilder<string, string>(config).Build();
                })
                .AddHostedService<OutboxProcessor>()
                .AddHostedService<IntegrationEventConsumer>()
                .AddSendrNotification()
                // InboxCommitDecorator 包在每個 handler 外面,處理完就自動
                // MarkProcessedAsync + CommitAsync——是這裡、每個訂閱各自決定要套用,
                // 不是 AddIntegrationEventSubscription 本身綁死的行為。
                .AddIntegrationEventSubscription<InboundOrderCreatedIntegrationEvent,
                    InboundOrderCreatedIntegrationEventHandler>(h => h.Decorator.With<InboxCommitDecorator>())
                .AddIntegrationEventSubscription<InboundOrderRejectedIntegrationEvent,
                    InboundOrderRejectedIntegrationEventHandler>(h => h.Decorator.With<InboxCommitDecorator>())
                .AddIntegrationEventSubscription<OutboundOrderCreatedIntegrationEvent,
                    OutboundOrderCreatedIntegrationEventHandler>(h => h.Decorator.With<InboxCommitDecorator>())
                .AddIntegrationEventSubscription<OutboundOrderRejectedIntegrationEvent,
                    OutboundOrderRejectedIntegrationEventHandler>(h => h.Decorator.With<InboxCommitDecorator>())
                .AddNpgsqlDataSource(configuration.GetConnectionString("DefaultConnection")!);

            return services;
        }
    }
}
