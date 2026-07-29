using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Organization.IntegrationTest;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("organization_db_test")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("RSA_PEM_PATH", GetPemPath());
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _container.GetConnectionString());

        await MigrationRunner.ApplyAsync(_container.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private static string GetPemPath([CallerFilePath] string sourceFile = "")
    {
        var testProjectDir = Path.GetDirectoryName(sourceFile)!;
        return Path.GetFullPath(Path.Combine(testProjectDir, "..", "..", "test-private.pem"));
    }
}
