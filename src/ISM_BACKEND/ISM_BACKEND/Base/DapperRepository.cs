using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ISM_BACKEND.Base;

public class DapperRepository : IDisposable
{
    private const int CommandTimeoutSeconds = 600;

    private readonly string _connectionString;
    private SqlConnection? _connection;
    private SqlTransaction? _transaction;

    public DapperRepository(IOptions<DatabaseSettings> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    private SqlConnection GetOpenConnection()
    {
        if (_transaction != null)
            return _transaction.Connection!;

        var conn = new SqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public void BeginTransaction()
    {
        _connection = new SqlConnection(_connectionString);
        _connection.Open();
        _transaction = _connection.BeginTransaction();
    }

    public void Commit()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _connection?.Dispose();
        _transaction = null;
        _connection = null;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _connection?.Dispose();
        _transaction = null;
        _connection = null;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        if (_transaction != null)
            return await _transaction.Connection!.QueryAsync<T>(sql, param, _transaction, CommandTimeoutSeconds);

        using var conn = GetOpenConnection();
        return await conn.QueryAsync<T>(sql, param, commandTimeout: CommandTimeoutSeconds);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        if (_transaction != null)
            return await _transaction.Connection!.QueryFirstOrDefaultAsync<T>(sql, param, _transaction, CommandTimeoutSeconds);

        using var conn = GetOpenConnection();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, param, commandTimeout: CommandTimeoutSeconds);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        if (_transaction != null)
            return await _transaction.Connection!.ExecuteAsync(sql, param, _transaction, CommandTimeoutSeconds);

        using var conn = GetOpenConnection();
        return await conn.ExecuteAsync(sql, param, commandTimeout: CommandTimeoutSeconds);
    }

    // 所有 PK 都是 BIGINT IDENTITY，用 SCOPE_IDENTITY() 取回自增值
    public async Task<long> ExecuteInsertWithIdentityAsync(string sql, object? param = null)
    {
        var insertSql = sql.TrimEnd().TrimEnd(';') + "; SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        if (_transaction != null)
            return await _transaction.Connection!.ExecuteScalarAsync<long>(insertSql, param, _transaction, CommandTimeoutSeconds);

        using var conn = GetOpenConnection();
        return await conn.ExecuteScalarAsync<long>(insertSql, param, commandTimeout: CommandTimeoutSeconds);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
