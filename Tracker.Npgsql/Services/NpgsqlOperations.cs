using Npgsql;
using System.Collections.Immutable;
using System.Data;
using Tracker.Core.Services.Contracts;

namespace Tracker.Npgsql.Services;

public sealed class NpgsqlOperations : ISourceOperations, IDisposable
{
    private readonly string _sourceId;
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;

    public NpgsqlOperations(string sourceId, NpgsqlDataSource dataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentNullException.ThrowIfNull(dataSource, nameof(dataSource));

        _sourceId = sourceId;
        _dataSource = dataSource;
    }

    public NpgsqlOperations(string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        _sourceId = sourceId;
        _dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
    }

    public string SourceId => _sourceId;

    public async Task<bool> EnableTracking(string key, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string tableName = "@table_name";
        const string query = $"SELECT enable_table_tracking({tableName});";

        await using var command = _dataSource.CreateCommand(query);
        command.Parameters.AddWithValue(tableName, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
            return await reader.GetFieldValueAsync<bool>(0, token);

        throw new InvalidOperationException($"Not able to enable tracking for table '{key}'");
    }
    public async Task<bool> DisableTracking(string key, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string tableName = "@table_name";
        const string query = $"SELECT disable_table_tracking({tableName});";

        await using var command = _dataSource.CreateCommand(query);
        command.Parameters.AddWithValue(tableName, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
            return await reader.GetFieldValueAsync<bool>(0, token);

        throw new InvalidOperationException($"Not able to disable tracking for table '{key}'");
    }

    public async Task<bool> IsTracked(string key, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string tableName = "@table_name";
        const string query = $"SELECT is_table_tracked({tableName});";

        await using var command = _dataSource.CreateCommand(query);
        command.Parameters.AddWithValue(tableName, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
            return await reader.GetFieldValueAsync<bool>(0, token);

        throw new InvalidOperationException($"Not able to detect tracking for table '{key}'");
    }

    public async Task<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string getTimestampQuery = "SELECT get_last_timestamp(@table_name);";
        await using var command = _dataSource.CreateCommand(getTimestampQuery);

        const string tableNameParam = "table_name";
        command.Parameters.AddWithValue(tableNameParam, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

        if (await reader.ReadAsync(token))
            return await reader.GetFieldValueAsync<DateTimeOffset?>(0, token) 
                ?? throw new NullReferenceException($"Not able to resolve timestamp for table '{key}'");

        throw new InvalidOperationException($"Not able to resolve timestamp for table '{key}'");
    }

    public async Task GetLastTimestamps(ImmutableArray<string> keys, DateTimeOffset[] timestamps, CancellationToken token)
    {
        if (keys.Length > timestamps.Length)
            throw new ArgumentException("Length timestamps array less then keys count");

        for (int i = 0; i < keys.Length; i++)
            timestamps[i] = await GetLastTimestamp(keys[i], token);
    }

    public async Task<DateTimeOffset> GetLastTimestamp(CancellationToken token)
    {
        const string getTimestampQuery = "SELECT pg_last_committed_xact();";
        await using var command = _dataSource.CreateCommand(getTimestampQuery);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

        if (await reader.ReadAsync(token))
            return await reader.GetFieldValueAsync<DateTimeOffset?>(1, token)
                ?? throw new NullReferenceException("Not able to resolve pg_last_committed_xact");

        throw new InvalidOperationException("Not able to resolve pg_last_committed_xact");
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

    ~NpgsqlOperations()
    {
        Dispose(disposing: false);
    }
}
