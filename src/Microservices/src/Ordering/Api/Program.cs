using Api.Configuration;
using Api.Endpoints.v1;
using Api.ExceptionHandling;
using Api.Helper;
using Api.Middleware;
using Confluent.Kafka;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservability(builder.Configuration);

builder.Services.AddApiVersioningWithOpenApi();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgresql")
    .AddKafka(new ProducerConfig { BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] }, name: "kafka")
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!, name: "redis");

builder.Services
    .AddJwtAuthentication(builder.Configuration)
    .AddOrderingAuthorization();

builder.Services
    .AddProblemDetailsWithCustomizeDetail()
    .AddValidation();

builder.Services
    .AddExceptionHandler<NpgsqlExceptionHandler>()
    .AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi().WithDocumentPerVersion();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CurrentUserMiddleware>();

var api = app.NewVersionedApi().MapGroup("/api/v{version:apiVersion}");
api.MapV1Endpoints();

app.MapHealthChecks("/healthz", new HealthCheckOptions { ResponseWriter = HealthCheckFormatHelper.WriteResponse });

app.Run();
