using Npgsql;
using System.Collections.Immutable;
using Tracker.Npgsql.Services;
using Tracker.Npgsql.Tests.Utils;

namespace Tracker.Npgsql.Tests;

public class NpgsqlOperationsIntegrationTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly NpgsqlDataSource _dataSource;
    private readonly NpgsqlOperations _operations;

    private readonly string _testTableName = "test_table_" + Guid.NewGuid().ToString("N");

    public NpgsqlOperationsIntegrationTests()
    {
        _connectionString = TestConfiguration.GetSqlConnectionString();
        _dataSource = new NpgsqlDataSourceBuilder(_connectionString).Build();
        _operations = new NpgsqlOperations("test-source", _connectionString);
    }

    public async Task InitializeAsync()
    {
        await SqlHelpers.CreateTestTable(_connectionString, _testTableName);
    }

    public async Task DisposeAsync()
    {
        _operations?.Dispose();
        _dataSource?.Dispose();
        await SqlHelpers.DropTable(_connectionString, _testTableName);
    }

    [Fact]
    public void Constructor_WithDataSource_InitializesCorrectly()
    {
        // Arrange & Act
        var ops = new NpgsqlOperations("test-id", _connectionString);

        // Assert
        Assert.Equal("test-id", ops.Id);
    }

    [Fact]
    public void Constructor_WithConnectionString_InitializesCorrectly()
    {
        // Arrange & Act
        var ops = new NpgsqlOperations("test-id", _connectionString);

        // Assert
        Assert.Equal("test-id", ops.Id);
        ops.Dispose();
    }

    [Fact]
    public void Constructor_NullSourceId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NpgsqlOperations(null, _connectionString));
        Assert.Throws<ArgumentException>(() => new NpgsqlOperations("", _connectionString));
    }

    [Fact]
    public void Constructor_NullDataSource_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NpgsqlOperations("test-id", (NpgsqlDataSource)null));
    }

    [Fact]
    public async Task EnableTracking_ValidTable_ReturnsTrue()
    {
        // Act
        var result = await _operations.EnableTracking(_testTableName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EnableTracking_Table_TwoTimes_ReturnsTrue()
    {
        // Act
        var result = await _operations.EnableTracking(_testTableName);
        var result1 = await _operations.EnableTracking(_testTableName);

        // Assert
        Assert.True(result);
        Assert.True(result1);
    }

    [Fact]
    public async Task EnableTracking_NullKey_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _operations.EnableTracking(null));
    }

    [Fact]
    public async Task EnableTracking_NotExistingDatabase_ThrowsException()
    {
        // Arrange
        using var operations = new NpgsqlOperations("non-existing-db-source", TestConfiguration.GetSqlNonExistingDatabaseConnectionString());

        // Act & Assert
        await Assert.ThrowsAsync<PostgresException>(async () =>
            await operations.EnableTracking(_testTableName));
    }

    [Fact]
    public async Task EnableTracking_NotExistingTable_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<PostgresException>(async () =>
            await _operations.EnableTracking(_testTableName + Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task EnableTracking_NotExistingExtensions_ThrowsException()
    {
        //Arrange
        var databaseName = "empty_database";
        var connectionString = TestConfiguration.GetGenericDatabaseConnectionString(databaseName);

        await SqlHelpers.CreateDatabase(_connectionString, databaseName);

        using var emptyDbOperations = new NpgsqlOperations(databaseName, connectionString);
        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<PostgresException>(async () =>
                await emptyDbOperations.EnableTracking(_testTableName));
        }
        finally
        {
            emptyDbOperations.Dispose();
            await SqlHelpers.DropDatabase(_connectionString, databaseName);
        }
    }

    [Fact]
    public async Task DisableTracking_ValidTable_ReturnsTrue()
    {
        // Act
        await _operations.EnableTracking(_testTableName);
        var result = await _operations.DisableTracking(_testTableName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DisableTracking_ValidTable_DisabledTracking_ReturnsFalse()
    {
        // Act
        var result = await _operations.DisableTracking(_testTableName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DisableTracking_NotExistingTable_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<PostgresException>(async () =>
            await _operations.DisableTracking(_testTableName + Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task DisableTracking_NotExistingDatabase_ThrowsException()
    {
        // Arrange
        using var operations = new NpgsqlOperations("non-existing-db-source", TestConfiguration.GetSqlNonExistingDatabaseConnectionString());

        // Act & Assert
        await Assert.ThrowsAsync<PostgresException>(async () =>
            await operations.DisableTracking(_testTableName));
    }

    [Fact]
    public async Task IsTracking_ValidTable_ReturnsBoolean()
    {
        // Act
        var result = await _operations.IsTracking(_testTableName);

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task GetLastVersion_TrackingNotEnabledTable_ThrowsException()
    {
        // Act
        await _operations.DisableTracking(_testTableName);
        Task timestamp() => _operations.GetLastVersion(_testTableName).AsTask();

        // Assert
        await Assert.ThrowsAsync<InvalidCastException>(timestamp);
    }

    [Fact]
    public async Task GetLastVersion_ValidTable_ReturnsTimestamp()
    {
        // Act
        await _operations.EnableTracking(_testTableName);
        var timestamp = await _operations.GetLastVersion(_testTableName);

        // Assert
        Assert.True(timestamp > 0);
    }

    [Fact]
    public async Task GetLastVersion_InvalidTable_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidTable = "non_existent_table_" + Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PostgresException>(async () =>
            await _operations.GetLastVersion(invalidTable));
    }

    [Fact]
    public async Task GetLastVersions_MultipleTables_ReturnsTimestamps()
    {
        // Arrange
        var tables = ImmutableArray.Create(
            _testTableName,
            _testTableName + "_2",
            _testTableName + "_3"
        );
        var versions = new long[tables.Length];

        // Create additional tables
        using var connection = await _dataSource.OpenConnectionAsync();
        for (int i = 1; i < tables.Length; i++)
        {
            using var cmd = new NpgsqlCommand(
                $"CREATE TABLE IF NOT EXISTS {tables[i]} (id SERIAL PRIMARY KEY)",
                connection);
            await cmd.ExecuteNonQueryAsync();
        }

        // Act
        await _operations.GetLastVersions(tables, versions);

        // Assert
        foreach (var version in versions)
        {
            Assert.True(version > 0);
        }

        // Cleanup
        for (int i = 1; i < tables.Length; i++)
        {
            using var cmd = new NpgsqlCommand($"DROP TABLE IF EXISTS {tables[i]}", connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task GetLastVersion_NoParameters_ReturnsDatabaseTimestamp()
    {
        // Act
        var timestamp = await _operations.GetLastVersion();

        // Assert
        Assert.True(timestamp > 0);
    }

    [Fact]
    public async Task SetLastVersion_ValidTable_ReturnsTrue()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);

        var testTimestamp = DateTimeOffset.UtcNow.AddHours(-1).Ticks;

        // Act
        var result = await _operations.SetLastVersion(_testTableName, testTimestamp);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SetLastVersion_DisabledTracking_ReturnsFalse()
    {
        // Arrange
        await _operations.DisableTracking(_testTableName);

        var testTimestamp = DateTimeOffset.UtcNow.AddHours(-1).Ticks;

        // Act
        var result = await _operations.SetLastVersion(_testTableName, testTimestamp);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Operations_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _operations.GetLastVersion(_testTableName, cts.Token));
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var ops = new NpgsqlOperations("dispose-test", _connectionString);

        // Act
        ops.Dispose();
        ops.Dispose(); // Second call should not throw

        // Assert
        // No exception thrown
    }

    [Fact]
    public async Task Operations_AfterDispose_ThrowObjectDisposedException()
    {
        // Arrange
        var ops = new NpgsqlOperations("dispose-test", _connectionString);
        ops.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await ops.GetLastVersion("test"));
    }
}