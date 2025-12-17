using Microsoft.Extensions.ObjectPool;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Immutable;
using System.Data;
using Tracker.Core.Services.Contracts;

namespace Tracker.Npgsql.Services;

public sealed class NpgsqlOperations : ISourceOperations, IDisposable
{
    private readonly string _sourceId;
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;

    private static readonly ObjectPool<NpgsqlParameter> _tableParamsPool =
        ObjectPool.Create(new ParameterObjectPolicy("table_name", NpgsqlDbType.Text));

    private static readonly ObjectPool<NpgsqlParameter> _timestampParamsPool =
        ObjectPool.Create(new ParameterObjectPolicy("timestamp", NpgsqlDbType.TimestampTz));

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

    public ValueTask<bool> EnableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string EnableTableTracking = "SELECT enable_table_tracking(@table_name);";

        using var command = _dataSource.CreateCommand(EnableTableTracking);

        var parameter = _tableParamsPool.Get();
        parameter.Value = key;
        command.Parameters.Add(parameter);

        try
        {
            using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
            var enabled = reader.Read() && reader.GetFieldValue<bool>(0);
            return new ValueTask<bool>(enabled);
        }
        finally
        {
            _tableParamsPool.Return(parameter);
        }
    }
    public ValueTask<bool> DisableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string DisableTableQuery = "SELECT disable_table_tracking(@table_name);";
        using var command = _dataSource.CreateCommand(DisableTableQuery);

        var parameter = _tableParamsPool.Get();
        parameter.Value = key;
        command.Parameters.Add(parameter);

        try
        {
            using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
            var disabled = reader.Read() && reader.GetFieldValue<bool>(0);
            return new ValueTask<bool>(disabled);
        }
        finally
        {
            _tableParamsPool.Return(parameter);
        }

    }

    public ValueTask<bool> IsTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string IsTrackingQuery = "SELECT is_table_tracked(@table_name);";

        using var command = _dataSource.CreateCommand(IsTrackingQuery);

        var parameter = _tableParamsPool.Get();
        parameter.Value = key;
        command.Parameters.Add(parameter);

        try
        {
            using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
            var tracking = reader.Read() && reader.GetFieldValue<bool>(0);
            return new ValueTask<bool>(tracking);
        }
        finally
        {
            _tableParamsPool.Return(parameter);
        }
    }

    public ValueTask<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token = default)
    {
        const string GetTimestampQuery = "SELECT get_last_timestamp(@table_name);";
        using var command = _dataSource.CreateCommand(GetTimestampQuery);

        var parameter = _tableParamsPool.Get();
        parameter.Value = key;
        command.Parameters.Add(parameter);

        try
        {
            using var reader = command.ExecuteReader(CommandBehavior.SingleRow);

            if (reader.Read())
            {
                var timestamp = reader.GetFieldValue<DateTimeOffset?>(0)
                   ?? throw new NullReferenceException($"Not able to resolve timestamp for table '{key}'");

                return new ValueTask<DateTimeOffset>(timestamp);
            }

            throw new InvalidOperationException($"Not able to resolve timestamp for table '{key}'");
        }
        finally
        {
            _tableParamsPool.Return(parameter);
        }
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
        const string GetTimestampQuery = "SELECT pg_last_committed_xact();";
        using var command = _dataSource.CreateCommand(GetTimestampQuery);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        if (reader.Read())
        {
            var result = reader.GetFieldValue<object[]?>(0);
            if (result is { Length: > 0 })
                return new ValueTask<DateTimeOffset>((DateTime)result[1]);
        }
        throw new InvalidOperationException("Not able to resolve pg_last_committed_xact");
    }

    public ValueTask<bool> SetLastTimestamp(string key, DateTimeOffset value, CancellationToken token = default)
    {
        const string SetTimestampQuery = $"SELECT set_last_timestamp(@table_name, @timestamp);";
        using var command = _dataSource.CreateCommand(SetTimestampQuery);

        var tableParameter = _tableParamsPool.Get();
        tableParameter.Value = key;
        command.Parameters.Add(tableParameter);

        var timestampParameter = _timestampParamsPool.Get();
        timestampParameter.Value = value;
        command.Parameters.Add(timestampParameter);

        try
        {
            using var reader = command.ExecuteReader(CommandBehavior.SingleRow);

            var setted = reader.Read() && reader.GetFieldValue<bool>(0);
            return new ValueTask<bool>(setted);
        }
        finally
        {
            _tableParamsPool.Return(tableParameter);
        }
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

    private sealed class ParameterObjectPolicy(string parameterName, NpgsqlDbType dbType) : IPooledObjectPolicy<NpgsqlParameter>
    {
        public NpgsqlParameter Create() => new(parameterName, dbType);

        public bool Return(NpgsqlParameter obj)
        {
            if (obj is null) return false;

            obj.Value = DBNull.Value;
            obj.Collection = null;

            return true;
        }
    }
}
