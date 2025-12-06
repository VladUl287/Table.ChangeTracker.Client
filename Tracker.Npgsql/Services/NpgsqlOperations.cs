using Npgsql;
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

    public async Task<DateTimeOffset?> GetLastTimestamp(string key, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string getTimestampQuery = "SELECT get_last_timestamp(@table_name);";
        await using var command = _dataSource.CreateCommand(getTimestampQuery);

        const string tableNameParam = "table_name";
        command.Parameters.AddWithValue(tableNameParam, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

        if (await reader.ReadAsync(token))
            return await reader.GetFieldValueAsync<DateTimeOffset?>(0, token);

        return null;
    }

    public Task<IEnumerable<DateTimeOffset>> GetLastTimestamp(ReadOnlySpan<string> keys, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<DateTimeOffset?> GetLastTimestamp(CancellationToken token)
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

    ~NpgsqlOperations()
    {
        Dispose(disposing: false);
    }
}
