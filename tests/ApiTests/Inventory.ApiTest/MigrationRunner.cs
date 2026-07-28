using System.Runtime.CompilerServices;
using Npgsql;

namespace Inventory.ApiTest;

// 直接透過 Npgsql 執行 *.up.sql 檔案,而不是呼叫外部的 `migrate` CLI ——
// 讓測試 fixture 保持自我包含(不需要任何外部工具在 PATH 上),
// 這樣在只有 .NET SDK 跟 Docker 的全新 CI runner 上也能跑。
internal static class MigrationRunner
{
    // 這裡綁定的是這個呼叫點(MigrationRunner.cs 自己內部),不是外部呼叫者的位置 ——
    // 所以無論從哪個子資料夾呼叫 ApplyAsync,解析出來的路徑都會是對的。
    private static string GetMigrationsDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", "..", "..", "src", "Inventory", "Migrations"));

    public static async Task ApplyAsync(string connectionString)
    {
        var migrationsDir = GetMigrationsDirectory();

        var upScripts = Directory.GetFiles(migrationsDir, "*.up.sql").OrderBy(f => f, StringComparer.Ordinal);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var script in upScripts)
        {
            var sql = await File.ReadAllTextAsync(script);
            await using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
