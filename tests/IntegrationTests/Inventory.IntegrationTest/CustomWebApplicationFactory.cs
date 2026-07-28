using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Inventory.IntegrationTest;

// Repository 測試只會直接從容器裡解析服務出來用 —— 從來不會真的發 HTTP request,
// 所以 JWT/authority 的設定完全不會被用到,不需要覆寫。
//
// 每次測試都起一個全新、用完就丟的 Postgres 容器,不依賴開發機(或 CI)上
// 事先手動建好、migrate 過的 inventory_db_test。
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("inventory_db_test")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Program.cs 會很早就讀取 ConnectionStrings:DefaultConnection,早於這個 factory 的
        // ConfigureWebHost hook 生效的時機 —— 在 host 建置前先設好環境變數,才能真的來得及
        // 生效(參考 Ordering 對應的 factory)。
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _container.GetConnectionString());

        await MigrationRunner.ApplyAsync(_container.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // OutboxProcessor / IntegrationEventConsumer 需要真正的 Kafka broker,跟這些
            // repository 層級的測試完全無關 —— 拿掉它們,測試建置/啟動 host 的時候
            // 才不會同時嘗試連 Kafka(然後失敗)。
            services.RemoveAll<IHostedService>();
        });
    }
}
