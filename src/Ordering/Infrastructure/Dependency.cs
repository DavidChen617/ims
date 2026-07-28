using Application;
using Application.Inbound;
using Application.Inbound.EventHandling;
using Application.Outbound;
using Application.Outbound.EventHandling;
using Application.Products;
using Confluent.Kafka;
using Dapper;
using Domain.InboundOrders;
using Domain.InboundOrders.Events;
using Domain.OutboundOrders;
using Domain.OutboundOrders.Events;
using Domain.Products;
using Infrastructure.Messaging;
using Infrastructure.Outbox;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Readers;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.TypeHandlers;
using MessageContract.OutboundOrders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Infrastructure;

public static class Dependency
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new ProductUnitTypeHandler());

            services
                .AddScoped<IOrderingUnitOfWork, OrderingUnitOfWork>()
                .AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IOrderingUnitOfWork>())
                .AddScoped<IAggregateRootChangeTracker, AggregateRootChangeTracker>()
                .AddScoped<IProductRepository, ProductRepository>()
                .AddScoped<IInboundOrderRepository, InboundOrderRepository>()
                .AddScoped<IOutboundOrderRepository, OutboundOrderRepository>()
                .AddScoped<IProductReader, ProductReader>()
                .AddScoped<IInboundOrderReader, InboundOrderReader>()
                .AddScoped<IOutboundOrderReader, OutboundOrderReader>()
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
                        GroupId = "ordering-service",
                        AutoOffsetReset = AutoOffsetReset.Earliest,
                        EnableAutoCommit = false
                    };

                    return new ConsumerBuilder<string, string>(config).Build();
                })
                .AddHostedService<OutboxProcessor>()
                .AddHostedService<IntegrationEventConsumer>()
                .AddSendrNotification()
                .AddNotificationHandler<InboundOrderCreatedDomainEvent>(
                    x => x.Handler.Sequence.With<InboundOrderCreatedDomainEventHandler>())
                .AddNotificationHandler<InboundOrderRejectedDomainEvent>(
                    x => x.Handler.Sequence.With<InboundOrderRejectedDomainEventHandler>())
                .AddNotificationHandler<OutboundOrderCreatedDomainEvent>(
                    x => x.Handler.Sequence.With<OutboundOrderCreatedDomainEventHandler>())
                .AddNotificationHandler<OutboundOrderRejectedDomainEvent>(
                    x => x.Handler.Sequence.With<OutboundOrderRejectedDomainEventHandler>())
                .AddIntegrationEventSubscription<OutboundInventoryReservedIntegrationEvent,
                    OutboundInventoryReservedIntegrationEventHandler>()
                .AddIntegrationEventSubscription<OutboundInventoryReservationFailedIntegrationEvent,
                    OutboundInventoryReservationFailedIntegrationEventHandler>()
                .AddNpgsqlDataSource(configuration.GetConnectionString("DefaultConnection")!);

            return services;
        }
    }
}
