using Microsoft.Data.SqlClient;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using Tracker.Core.Services.Contracts;

namespace Tracker.SqlServer.Services;

public sealed class SqlServerOperations : ISourceOperations, IDisposable
{
    private readonly string _sourceId;
    private readonly DbDataSource _dataSource;
    private bool _disposed;

    public SqlServerOperations(string sourceId, DbDataSource dataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentNullException.ThrowIfNull(dataSource, nameof(dataSource));

        _sourceId = sourceId;
        _dataSource = dataSource;
    }

    public SqlServerOperations(string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        _sourceId = sourceId;
        _dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);
    }

    public string SourceId => _sourceId;

    public async Task<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string query = $"""
            SELECT s.last_user_update
            FROM sys.dm_db_index_usage_stats s
            INNER JOIN sys.tables t ON s.object_id = t.object_id
            WHERE database_id = DB_ID() AND t.name = @table_name;
            """;

        await using var command = _dataSource.CreateCommand(query);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
            return await reader.GetFieldValueAsync<DateTime?>(0, token) ?? default;

        throw new InvalidOperationException($"Not able to enable tracking for table '{key}'");
    }

    public async Task GetLastTimestamps(ImmutableArray<string> keys, DateTimeOffset[] timestamps, CancellationToken token)
    {
        if (keys.Length > timestamps.Length)
            throw new ArgumentException("Length timestamps array less then keys count");

        for (int i = 0; i < keys.Length; i++)
            timestamps[i] = await GetLastTimestamp(keys[i], token);
    }

    public Task<DateTimeOffset> GetLastTimestamp(CancellationToken token)
    {
        throw new NotImplementedException();
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

    public Task<bool> EnableTracking(string key, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DisableTracking(string key, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsTracked(string key, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    ~SqlServerOperations()
    {
        Dispose(disposing: false);
    }
}
