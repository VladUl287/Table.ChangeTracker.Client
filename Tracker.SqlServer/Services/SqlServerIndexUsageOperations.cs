using Microsoft.Data.SqlClient;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using Tracker.Core.Services.Contracts;

namespace Tracker.SqlServer.Services;

/// <summary>
/// Provides read-only access to SQL Server index usage statistics for tracking purposes.
/// This class queries the sys.dm_db_index_usage_stats DMV to retrieve the last update timestamps
/// for tables and databases. Tracking is always enabled and cannot be disabled.
/// </summary>
/// <remarks>
/// This implementation is designed for monitoring scenarios where you need to track
/// when database indexes were last used. It provides read-only access to SQL Server's
/// index usage statistics.
/// </remarks>
public sealed class SqlServerIndexUsageOperations : ISourceOperations, IDisposable
{
    private readonly string _sourceId;
    private readonly DbDataSource _dataSource;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerIndexUsageMonitor"/> class
    /// with the specified source identifier and database data source.
    /// </summary>
    /// <param name="sourceId">A unique identifier for this monitoring source.</param>
    /// <param name="dataSource">The database data source to use for queries.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sourceId"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataSource"/> is null.</exception>
    public SqlServerIndexUsageOperations(string sourceId, DbDataSource dataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentNullException.ThrowIfNull(dataSource, nameof(dataSource));

        _sourceId = sourceId;
        _dataSource = dataSource;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerIndexUsageMonitor"/> class
    /// with the specified source identifier and connection string.
    /// </summary>
    /// <param name="sourceId">A unique identifier for this monitoring source.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sourceId"/> or <paramref name="connectionString"/> is null or empty.</exception>
    public SqlServerIndexUsageOperations(string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        _sourceId = sourceId;
        _dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);
    }

    /// <summary>
    /// Gets the unique identifier for this monitoring source.
    /// </summary>
    public string SourceId => _sourceId;

    /// <summary>
    /// Retrieves the last update timestamp for a specific table by querying index usage statistics.
    /// </summary>
    /// <param name="key">The name of the table to query.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{DateTimeOffset}"/> that contains the last update timestamp for the specified table.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve the timestamp for the specified table.</exception>
    /// <exception cref="NullReferenceException">Thrown when the retrieved timestamp is null.</exception>
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
                throw new InvalidOperationException($"Unable to retrieve timestamp for table '{key}'. No index usage data found.");

            return new ValueTask<DateTimeOffset>(timestamp);
        }
        throw new InvalidOperationException($"Table '{key}' not found or has no index usage statistics.");
    }

    /// <summary>
    /// Retrieves the last update timestamps for multiple tables in bulk.
    /// </summary>
    /// <param name="keys">An immutable array of table names to query.</param>
    /// <param name="timestamps">An array to store the retrieved timestamps. Must have at least the same length as <paramref name="keys"/>.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="timestamps"/> array length is less than <paramref name="keys"/> array length.</exception>
    public async ValueTask GetLastTimestamps(ImmutableArray<string> keys, DateTimeOffset[] timestamps, CancellationToken token = default)
    {
        if (keys.Length > timestamps.Length)
            throw new ArgumentException($"Timestamps array length ({timestamps.Length}) must be at least as large as keys count ({keys.Length}).", nameof(timestamps));

        for (int i = 0; i < keys.Length; i++)
            timestamps[i] = await GetLastTimestamp(keys[i], token);
    }

    /// <summary>
    /// Retrieves the most recent update timestamp across all tables in the current database.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{DateTimeOffset}"/> that contains the most recent update timestamp in the database.</returns>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve the timestamp for the database.</exception>
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
                throw new InvalidOperationException("Unable to retrieve timestamp for database. No index usage data found.");

            return new ValueTask<DateTimeOffset>(timestamp);
        }

        throw new InvalidOperationException("Unable to retrieve timestamp for database.");
    }

    /// <summary>
    /// Enables tracking for the specified table. This implementation always returns true as tracking is inherently enabled.
    /// </summary>
    /// <returns>A <see cref="ValueTask{Boolean}"/> that always returns true.</returns>
    public ValueTask<bool> EnableTracking(string key, CancellationToken token = default) => ValueTask.FromResult(true);

    /// <summary>
    /// Checks if tracking is enabled for the specified table. This implementation always returns true.
    /// </summary>
    /// <returns>A <see cref="ValueTask{Boolean}"/> that always returns true.</returns>
    public ValueTask<bool> IsTracking(string key, CancellationToken token = default) => ValueTask.FromResult(true);

    /// <summary>
    /// Attempts to disable tracking for the specified table. This operation is not supported.
    /// </summary>
    /// <returns>A <see cref="ValueTask{Boolean}"/> that always throws an <see cref="InvalidOperationException"/>.</returns>
    /// <exception cref="InvalidOperationException">Always thrown, as tracking cannot be disabled for index usage statistics.</exception>
    public ValueTask<bool> DisableTracking(string key, CancellationToken token = default) =>
        throw new InvalidOperationException("Tracking cannot be disabled. SQL Server index usage statistics are always collected by the database engine.");

    /// <summary>
    /// Attempts to set a custom timestamp for the specified table. This operation is not supported.
    /// </summary>
    /// <returns>A <see cref="ValueTask{Boolean}"/> that always throws an <see cref="InvalidOperationException"/>.</returns>
    /// <exception cref="InvalidOperationException">Always thrown, as index usage statistics are read-only and maintained by SQL Server.</exception>
    public ValueTask<bool> SetLastTimestamp(string key, DateTimeOffset value, CancellationToken token = default) =>
        throw new InvalidOperationException("Cannot set timestamp. SQL Server index usage statistics are read-only and managed by the database engine.");

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
