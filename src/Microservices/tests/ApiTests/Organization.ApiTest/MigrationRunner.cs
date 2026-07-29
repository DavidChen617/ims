using System.Runtime.CompilerServices;
using Npgsql;

namespace Organization.ApiTest;

internal static class MigrationRunner
{
    private static string GetMigrationsDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", "..", "..", "src", "Organization", "Migrations"));

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
