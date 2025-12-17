using Microsoft.Data.SqlClient;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using Tracker.Core.Services.Contracts;

namespace Tracker.SqlServer.Services;

public sealed class SqlServerIndexUsageStatsOperations : ISourceOperations, IDisposable
{
    private readonly string _sourceId;
    private readonly DbDataSource _dataSource;
    private bool _disposed;

    public SqlServerIndexUsageStatsOperations(string sourceId, DbDataSource dataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentNullException.ThrowIfNull(dataSource, nameof(dataSource));

        _sourceId = sourceId;
        _dataSource = dataSource;
    }

    public SqlServerIndexUsageStatsOperations(string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        _sourceId = sourceId;
        _dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);
    }

    public string SourceId => _sourceId;

    public ValueTask<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string GetLastTimestampQuery = $"""
            SELECT s.last_user_update
            FROM sys.dm_db_index_usage_stats s
            INNER JOIN sys.tables t ON s.object_id = t.object_id
            WHERE database_id = DB_ID() AND t.name = @table_name;
            """;

        using var command = _dataSource.CreateCommand(GetLastTimestampQuery);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        if (reader.Read())
        {
            var timestamp = reader.GetFieldValue<DateTimeOffset?>(0) ??
                throw new NullReferenceException($"Not able to enable tracking for table '{key}'");

            return new ValueTask<DateTimeOffset>(timestamp);
        }
        throw new InvalidOperationException($"Not able to enable tracking for table '{key}'");
    }

    public async ValueTask GetLastTimestamps(ImmutableArray<string> keys, DateTimeOffset[] timestamps, CancellationToken token = default)
    {
        if (keys.Length > timestamps.Length)
            throw new ArgumentException("Length timestamps array less then keys count");

        for (int i = 0; i < keys.Length; i++)
            timestamps[i] = await GetLastTimestamp(keys[i], token);
    }

    public ValueTask<DateTimeOffset> GetLastTimestamp(CancellationToken token = default)
    {
        const string query = $"""
            SELECT MAX(last_user_update)
            FROM sys.dm_db_index_usage_stats
            WHERE database_id = DB_ID();
            """;

        using var command = _dataSource.CreateCommand(query);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        if (reader.Read())
        {
            var timestamp = reader.GetFieldValue<DateTimeOffset?>(0) ??
                throw new NullReferenceException("Not able to get last timestamp for database");

            return new ValueTask<DateTimeOffset>(timestamp);
        }

        throw new InvalidOperationException("Not able to get last timestamp for database");
    }

    public ValueTask<bool> EnableTracking(string key, CancellationToken token = default) => ValueTask.FromResult(true);

    public ValueTask<bool> IsTracking(string key, CancellationToken token = default) => ValueTask.FromResult(true);

    public ValueTask<bool> DisableTracking(string key, CancellationToken token = default)
    {
        throw new InvalidOperationException("Always enabled?");
    }

    public ValueTask<bool> SetLastTimestamp(string key, DateTimeOffset value, CancellationToken token = default)
    {
        throw new InvalidOperationException("Readonly");
    }

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

    ~SqlServerIndexUsageStatsOperations()
    {
        Dispose(disposing: false);
    }
}
