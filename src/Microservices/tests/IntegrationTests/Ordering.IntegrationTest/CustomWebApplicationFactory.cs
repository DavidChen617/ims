using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ordering.IntegrationTest;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("ordering_db_test")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8").Build();

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
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
        });
    }
}
