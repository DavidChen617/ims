using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Organization.IntegrationTest;

// Program.cs 會在 Main 最前面就讀取 RSA_PEM_PATH、建出 RSA 金鑰,早於
// WebApplicationBuilder.Build() 執行 —— 這個時機比 WebApplicationFactory 的
// ConfigureAppConfiguration/ConfigureWebHost hook 還早,那兩個 hook 完全來不及生效。
// 環境變數是 WebApplication.CreateBuilder 自己就會讀的,所以是唯一能在這麼早的
// 讀取時機前就生效的覆寫方式。
//
// 每次測試都起一個全新、用完就丟的 Postgres 容器,不依賴開發機(或 CI)上
// 事先手動建好、migrate 過的 organization_db_test。
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
