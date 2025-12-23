using Npgsql;
using System.Collections.Immutable;
using System.Data;
using Tracker.Core.Services.Contracts;
using Tracker.Npgsql.Extensions;

namespace Tracker.Npgsql.Services;

public sealed class NpgsqlOperations : ISourceProvider
{
    private readonly string _sourceId;
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;

    private const string TABLE_NAME_PARAM = "table_name";
    private const string TIMESTAMP_PARAM = "timestamp";

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

    public string Id => _sourceId;

    public async ValueTask<bool> EnableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string EnableTableTracking = "SELECT enable_table_tracking(@table_name);";
        using var command = _dataSource.CreateCommand(EnableTableTracking);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        return
            await reader.ReadAsync(token) &&
            await reader.GetFieldValueAsync<bool>(0, token);
    }
    public async ValueTask<bool> DisableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string DisableTableQuery = "SELECT disable_table_tracking(@table_name);";
        using var command = _dataSource.CreateCommand(DisableTableQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        return
            await reader.ReadAsync(token) &&
            await reader.GetFieldValueAsync<bool>(0, token);
    }

    public async ValueTask<bool> IsTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string IsTrackingQuery = "SELECT is_table_tracked(@table_name);";
        using var command = _dataSource.CreateCommand(IsTrackingQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        return
            await reader.ReadAsync(token) &&
            await reader.GetFieldValueAsync<bool>(0, token);
    }

    public async ValueTask<long> GetLastVersion(string key, CancellationToken token = default)
    {
        const string GetTimestampQuery = "SELECT get_last_timestamp(@table_name);";
        using var command = _dataSource.CreateCommand(GetTimestampQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
        {
            var test = reader.HasRows;
            return reader.GetTimestampTicks(0);
        }

        throw new InvalidOperationException($"Not able to resolve timestamp for table '{key}'");
    }

    public async ValueTask GetLastVersions(ImmutableArray<string> keys, long[] versions, CancellationToken token = default)
    {
        const string GetTimestampQuery = "SELECT get_last_timestamps(@table_name);";
        using var command = _dataSource.CreateCommand(GetTimestampQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text, keys);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
        {
            var timestamps = await reader.GetFieldValueAsync<DateTimeOffset?[]>(0);
            for (int i = 0; i < timestamps.Length; i++)
            {
                var timestamp = timestamps[i]
                    ?? throw new NullReferenceException($"Not able to resolve timestamp for table '{keys[i]}'");
                versions[i] = timestamp.Ticks;
            }
            return;
        }

        throw new InvalidOperationException($"Not able to resolve timestamp for tables");
    }

    public async ValueTask<long> GetLastVersion(CancellationToken token = default)
    {
        const string GetTimestampQuery = "SELECT (pg_last_committed_xact()).timestamp;";
        using var command = _dataSource.CreateCommand(GetTimestampQuery);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
            return reader.GetTimestampTicks(0);

        throw new InvalidOperationException("Not able to resolve pg_last_committed_xact timestamp");
    }

    public async ValueTask<bool> SetLastVersion(string key, long value, CancellationToken token = default)
    {
        const string SetTimestampQuery = $"SELECT set_last_timestamp(@table_name, @timestamp);";
        using var command = _dataSource.CreateCommand(SetTimestampQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);
        command.Parameters.AddWithValue(TIMESTAMP_PARAM, new DateTimeOffset(value, TimeSpan.Zero));

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        return
            await reader.ReadAsync(token) &&
            await reader.GetFieldValueAsync<bool>(0);
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

    ~NpgsqlOperations() => Dispose(disposing: false);
}
