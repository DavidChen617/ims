using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace Inventory.ApiTest;

// Inventory 驗證 JWT 的方式是透過 OIDC discovery 對 `JwtSetting:Authority` 打過去,而這些
// Organization 發的 token —— 但測試時 Organization 根本沒有跑起來。這個 factory 直接重用
// `dotnet user-jwts` 存在 Inventory.Api 的 user-secrets 裡的簽章金鑰(見 TestJwt),
// 把 bearer handler 換成直接用這把金鑰驗證,完全繞過 Authority。
//
// 每次測試都起一個全新、用完就丟的 Postgres 容器,不依賴開發機(或 CI)上事先手動建好、migrate 過的 inventory_db_test。
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "dotnet-user-jwts";
    public const string Audience = "wms";

    public SymmetricSecurityKey SigningKey { get; private set; } = null!;

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("inventory_db_test")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Program.cs 會很早就讀取 ConnectionStrings:DefaultConnection,早於這個 factory 的
        // ConfigureWebHost hook 生效的時機 —— 在 host 建置前先設好環境變數,才能真的來得及生效。
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _container.GetConnectionString());

        await MigrationRunner.ApplyAsync(_container.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
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
                                "No dev signing key found for Inventory.Api. Run: dotnet user-jwts key --project src/Inventory/Api");
            SigningKey = new SymmetricSecurityKey(Convert.FromBase64String(keyBase64));

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;
                // MapInboundClaims 預設是 true,跟正式環境一致 —— 它會把 "sub" 重新映射成
                // ClaimTypes.NameIdentifier,而這正是 CurrentUserMiddleware 讀取的 claim。
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
