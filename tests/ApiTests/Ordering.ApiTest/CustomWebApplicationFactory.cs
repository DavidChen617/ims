using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ordering.ApiTest;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "dotnet-user-jwts";
    public const string Audience = "wms";

    public SymmetricSecurityKey SigningKey { get; private set; } = null!;

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("ordering_db_test")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_container.StartAsync(), _redisContainer.StartAsync());

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _container.GetConnectionString());
        Environment.SetEnvironmentVariable("Redis__ConnectionString", _redisContainer.GetConnectionString());

        await MigrationRunner.ApplyAsync(_container.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(_container.DisposeAsync().AsTask(), _redisContainer.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices((context, services) =>
        {
            // OutboxProcessor / IntegrationEventConsumer 需要真正的 Kafka broker,跟這些
            // endpoint 測試要驗證的東西無關 —— 拿掉它們,host 才不會在測試跑的時候
            // 在背景嘗試連 Kafka(然後失敗)。
            services.RemoveAll<IHostedService>();

            var keyBase64 = context.Configuration["Authentication:Schemes:Bearer:SigningKeys:0:Value"]
                ?? throw new InvalidOperationException(
                    "No dev signing key found for Ordering.Api. Run: dotnet user-jwts key --project src/Ordering/Api");
            SigningKey = new SymmetricSecurityKey(Convert.FromBase64String(keyBase64));

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = SigningKey
                };
            });
        });
    }
}
