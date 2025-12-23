using Microsoft.Data.SqlClient;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using Tracker.Core.Services.Contracts;

namespace Tracker.SqlServer.Services;

public sealed class SqlServerIndexUsageOperations : ISourceProvider
{
    private readonly string _sourceId;
    private readonly DbDataSource _dataSource;
    private bool _disposed;

    public SqlServerIndexUsageOperations(string sourceId, DbDataSource dataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentNullException.ThrowIfNull(dataSource, nameof(dataSource));

        _sourceId = sourceId;
        _dataSource = dataSource;
    }

    public SqlServerIndexUsageOperations(string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        _sourceId = sourceId;
        _dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);
    }

    public string Id => _sourceId;

    public async ValueTask<long> GetLastVersion(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string GetLastTimestampQuery = $"""
            DECLARE @table_id INT = OBJECT_ID(@table_name);

            IF @table_id IS NULL
                THROW 51000, 'Table does not exist', 1;

            SELECT s.last_user_update
            FROM sys.dm_db_index_usage_stats s
            WHERE s.database_id = DB_ID() AND s.object_id = @table_id AND s.last_user_update IS NOT NULL;
            """;

        using var command = _dataSource.CreateCommand(GetLastTimestampQuery);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

        if (await reader.ReadAsync(token))
            return reader.GetDateTime(0).Ticks;

        return default;
    }

    public async ValueTask GetLastVersions(ImmutableArray<string> keys, long[] versions, CancellationToken token = default)
    {
        if (keys.Length > versions.Length)
            throw new ArgumentException($"Timestamps array length ({versions.Length}) must be at least as large as keys count ({keys.Length}).", nameof(versions));

        for (int i = 0; i < keys.Length; i++)
            versions[i] = await GetLastVersion(keys[i], token);
    }

    public async ValueTask<long> GetLastVersion(CancellationToken token = default)
    {
        const string query = $"""
            SELECT MAX(last_user_update)
            FROM sys.dm_db_index_usage_stats
            WHERE database_id = DB_ID();
            """;

        using var command = _dataSource.CreateCommand(query);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
        {
            if (await reader.IsDBNullAsync(0, token))
                return default;

            return reader.GetDateTime(0).Ticks;
        }

        throw new InvalidOperationException("Unable to retrieve timestamp for database.");
    }

    public ValueTask<bool> EnableTracking(string key, CancellationToken token = default) => ValueTask.FromResult(true);

    public ValueTask<bool> IsTracking(string key, CancellationToken token = default) => ValueTask.FromResult(true);

    public ValueTask<bool> DisableTracking(string key, CancellationToken token = default) =>
        throw new InvalidOperationException("Tracking cannot be disabled. SQL Server index usage statistics are always collected by the database engine.");

    public ValueTask<bool> SetLastVersion(string key, long value, CancellationToken token = default) =>
        throw new InvalidOperationException("Cannot set version. SQL Server index usage statistics are read-only and managed by the database engine.");

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            _dataSource?.Dispose();

        _disposed = true;
    }

    ~SqlServerIndexUsageOperations() => Dispose(disposing: false);
}
