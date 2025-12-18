using Microsoft.Data.SqlClient;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Tracker.Core.Services.Contracts;

[assembly: InternalsVisibleTo("Tracker.SqlServer.Tests")]

namespace Tracker.SqlServer.Services;

public sealed class SqlServerChangeTrackingOperations : ISourceOperations, IDisposable
{
    private readonly string _sourceId;
    private readonly DbDataSource _dataSource;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerChangeTrackingOperations"/> class
    /// with the specified source identifier and database data source.
    /// </summary>
    /// <param name="sourceId">A unique identifier for this monitoring source.</param>
    /// <param name="dataSource">The database data source to use for queries.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sourceId"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataSource"/> is null.</exception>
    public SqlServerChangeTrackingOperations(string sourceId, DbDataSource dataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentNullException.ThrowIfNull(dataSource, nameof(dataSource));

        _sourceId = sourceId;
        _dataSource = dataSource;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerChangeTrackingOperations"/> class
    /// with the specified source identifier and connection string.
    /// </summary>
    /// <param name="sourceId">A unique identifier for this monitoring source.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sourceId"/> or <paramref name="connectionString"/> is null or empty.</exception>
    public SqlServerChangeTrackingOperations(string sourceId, string connectionString)
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
    /// Retrieves the last change tracking version for a specific table.
    /// </summary>
    /// <param name="key">The name of the table to query.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{long}"/> that contains the last change tracking version for the specified table.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve the version for the specified table.</exception>
    public async ValueTask<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string GetLastVersionQuery = """
            DECLARE @current_version BIGINT = CHANGE_TRACKING_CURRENT_VERSION();
            DECLARE @min_valid_version BIGINT = CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(@table_name));
            DECLARE @last_change_version BIGINT = NULL;
            
            DECLARE @sql NVARCHAR(MAX) = N'
                SELECT TOP 1 @last_change = SYS_CHANGE_VERSION 
                FROM CHANGETABLE(CHANGES ' + QUOTENAME(@table_name) + ', 0) as c 
                ORDER BY SYS_CHANGE_VERSION DESC';
                
            DECLARE @params NVARCHAR(MAX) = N'@last_change BIGINT OUTPUT';
            EXEC sp_executesql @sql, @params, @last_change = @last_change_version OUTPUT;
            
            SELECT 
                @current_version as current_version,
                @min_valid_version as min_valid_version,
                @last_change_version as last_change_version;
            """;

        await using var command = _dataSource.CreateCommand(GetLastVersionQuery);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
            if (await reader.ReadAsync(token))
            {
                var currentVersion = reader.GetInt64(0);
                var minValidVersion = reader.GetInt64(1);
                var lastChangeVersion = reader.IsDBNull(2) ? (long?)null : reader.GetInt64(2);

                // If there are changes, use the last change version
                // Otherwise, use the current version (which represents the database state)
                var version = lastChangeVersion ?? currentVersion;

                // Convert version to DateTimeOffset (approximation - change tracking versions are sequential numbers)
                // TODO: Change tracking versions don't directly map to timestamps
                var timestamp = DateTimeOffset.UtcNow.AddTicks(-version);
                return timestamp;
            }

            throw new InvalidOperationException($"Table '{key}' not found or change tracking is not enabled.");
        }
        catch (SqlException sqlException)
        {
            throw new InvalidOperationException(sqlException.Message);
        }
    }

    /// <summary>
    /// Retrieves the last change tracking versions for multiple tables in bulk.
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
    /// Retrieves the most recent change tracking version across all tables with change tracking enabled.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{DateTimeOffset}"/> that contains the most recent change tracking version in the database.</returns>
    public async ValueTask<DateTimeOffset> GetLastTimestamp(CancellationToken token = default)
    {
        const string query = """
            SELECT CHANGE_TRACKING_CURRENT_VERSION() as current_version
            """;

        await using var command = _dataSource.CreateCommand(query);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

        if (await reader.ReadAsync(token))
        {
            var currentVersion = reader.GetInt64(0);
            // Convert version to DateTimeOffset (approximation)
            var timestamp = DateTimeOffset.UtcNow.AddTicks(-currentVersion);
            return timestamp;
        }

        throw new InvalidOperationException("Unable to retrieve change tracking version for database.");
    }

    /// <summary>
    /// Enables change tracking for the specified table.
    /// </summary>
    /// <returns>A <see cref="ValueTask{Boolean}"/> that returns true if tracking was enabled, false otherwise.</returns>
    public async ValueTask<bool> EnableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string EnableTrackingQuery = """
            IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(@table_name))
            BEGIN
                DECLARE @sql NVARCHAR(MAX) = N'ALTER TABLE ' + QUOTENAME(@table_name) + ' ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON)';
                EXEC sp_executesql @sql;
                SELECT 1;
            END
            ELSE
                SELECT 1;
            """;

        await using var command = _dataSource.CreateCommand(EnableTrackingQuery);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(token);
        return result is not null;
    }

    /// <summary>
    /// Checks if change tracking is enabled for the specified table.
    /// </summary>
    /// <returns>A <see cref="ValueTask{Boolean}"/> that returns true if tracking is enabled, false otherwise.</returns>
    public async ValueTask<bool> IsTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string checkTrackingQuery = """
            SELECT COUNT(1) 
            FROM sys.change_tracking_tables 
            WHERE object_id = OBJECT_ID(@table_name)
            """;

        await using var command = _dataSource.CreateCommand(checkTrackingQuery);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(token);
        return result != null && Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// Attempts to disable change tracking for the specified table.
    /// </summary>
    /// <returns>A <see cref="ValueTask{Boolean}"/> that returns true if tracking was disabled, false otherwise.</returns>
    public async ValueTask<bool> DisableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        try
        {
            string disableTrackingQuery = $"""
                IF EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(@table_name))
                BEGIN
                    ALTER TABLE [{key}] DISABLE CHANGE_TRACKING;
                    SELECT 1;
                END
                ELSE
                    SELECT 0;
                """;

            await using var command = _dataSource.CreateCommand(disableTrackingQuery);
            var parameter = command.CreateParameter();
            parameter.ParameterName = "table_name";
            parameter.Value = key;
            parameter.DbType = DbType.String;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(token);
            return result != null && Convert.ToInt32(result) > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to set a custom timestamp for the specified table. This operation is not supported for change tracking.
    /// </summary>
    /// <returns>A <see cref="ValueTask{Boolean}"/> that always throws an <see cref="InvalidOperationException"/>.</returns>
    /// <exception cref="InvalidOperationException">Always thrown, as change tracking versions are managed by SQL Server.</exception>
    public ValueTask<bool> SetLastTimestamp(string key, DateTimeOffset value, CancellationToken token = default) =>
        throw new InvalidOperationException("Cannot set timestamp. SQL Server change tracking versions are managed by the database engine.");

    /// <summary>
    /// Gets the minimum valid version for a table's change tracking.
    /// </summary>
    /// <param name="key">The name of the table.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>The minimum valid version number.</returns>
    public async ValueTask<long> GetMinValidVersion(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string query = "SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(@table_name))";

        await using var command = _dataSource.CreateCommand(query);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(token);
        return result != DBNull.Value ? Convert.ToInt64(result) : 0;
    }

    /// <summary>
    /// Gets the current change tracking version for the database.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <returns>The current change tracking version.</returns>
    public async ValueTask<long> GetCurrentVersion(CancellationToken token = default)
    {
        const string query = "SELECT CHANGE_TRACKING_CURRENT_VERSION()";

        await using var command = _dataSource.CreateCommand(query);
        var result = await command.ExecuteScalarAsync(token);
        return result != DBNull.Value ? Convert.ToInt64(result) : 0;
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

    ~SqlServerChangeTrackingOperations() => Dispose(disposing: false);
}
