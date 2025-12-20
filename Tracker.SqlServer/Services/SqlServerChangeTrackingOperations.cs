using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Collections.Immutable;
using Tracker.Core.Services.Contracts;

namespace Tracker.SqlServer.Services;

public sealed class SqlServerChangeTrackingOperations : ISourceOperations, IDisposable
{
    private readonly string _sourceId;
    private readonly DbDataSource _dataSource;
    private bool _disposed;

    public SqlServerChangeTrackingOperations(string sourceId, DbDataSource dataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentNullException.ThrowIfNull(dataSource, nameof(dataSource));

        _sourceId = sourceId;
        _dataSource = dataSource;
    }

    public SqlServerChangeTrackingOperations(string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        _sourceId = sourceId;
        _dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);
    }

    public string SourceId => _sourceId;

    public async ValueTask<long> GetLastVersion(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        string getLastVersionQuery = $"SELECT ISNULL(MAX(SYS_CHANGE_VERSION), 0) FROM CHANGETABLE(CHANGES [{key}], 0) as c";

        await using var command = _dataSource.CreateCommand(getLastVersionQuery);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

            if (await reader.ReadAsync(token))
                return reader.GetInt64(0);

            throw new InvalidOperationException("Unable to get last version for table");
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
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
        const string GetCurrentVersionQuery = "SELECT CHANGE_TRACKING_CURRENT_VERSION()";

        await using var command = _dataSource.CreateCommand(GetCurrentVersionQuery);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

        if (await reader.ReadAsync(token))
            return reader.GetInt64(0);

        throw new InvalidOperationException("Unable to retrieve change tracking version for database.");
    }

    public async ValueTask<bool> IsTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string IsTrackingQuery = "SELECT COUNT(1) FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(@table_name)";

        await using var command = _dataSource.CreateCommand(IsTrackingQuery);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
            return reader.GetInt32(0) > 0;

        throw new InvalidOperationException("Unable to retrieve change tracking flag for table.");
    }

    public async ValueTask<bool> EnableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        string enableTrackingQuery = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('{key}'))
            BEGIN
                ALTER TABLE {key} ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
                SELECT 1;
            END
            ELSE
                SELECT 0;
            """;

        await using var command = _dataSource.CreateCommand(enableTrackingQuery);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
            return reader.GetInt32(0) > 0;

        throw new InvalidOperationException("Unable to enable change tracking for table.");
    }

    public async ValueTask<bool> DisableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        string disableTrackingQuery = $"""
            IF EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('{key}'))
            BEGIN
                ALTER TABLE {key} DISABLE CHANGE_TRACKING;
                SELECT 1;
            END
            ELSE
                SELECT 0;
            """;

        await using var command = _dataSource.CreateCommand(disableTrackingQuery);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (await reader.ReadAsync(token))
            return reader.GetInt32(0) > 0;

        throw new InvalidOperationException("Unable to disable change tracking for table.");
    }

    public ValueTask<bool> SetLastVersion(string key, long version, CancellationToken token = default) =>
        throw new InvalidOperationException("Cannot set version. SQL Server change tracking versions are managed by the database engine.");

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

    ~SqlServerChangeTrackingOperations() => Dispose(disposing: false);
}
