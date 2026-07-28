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
    .AddKafka(new ProducerConfig { BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] }, name: "kafka");

builder.Services
    .AddJwtAuthentication(builder.Configuration)
    .AddOrderingAuthorization();

builder.Services
    .AddProblemDetailsWithCustomizeDetail()
    .AddValidation();

// 依序輪詢:先給 NpgsqlExceptionHandler 機會接住漏檢查就直接打到 DB 的例外,
// 其他所有沒被特化處理的例外最後都掉到 GlobalExceptionHandler。
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
