using Microsoft.Data.SqlClient;
using System.Collections.Immutable;
using System.Data.Common;
using Tracker.SqlServer.Services;
using Xunit.Abstractions;

namespace Tracker.SqlServer.Tests;

public class SqlServerChangeTrackingOperationsTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly string _connectionString;
    private DbDataSource _dataSource;
    private DbDataSource _dataSourceLowPrivilage;
    private SqlServerChangeTrackingOperations _operations;
    private SqlServerChangeTrackingOperations _lowPrivilageOperations;
    private readonly string _testTableName = $"TestTable_{Guid.NewGuid():N}";
    private readonly string _testTableName2 = $"TestTable2_{Guid.NewGuid():N}";

    public SqlServerChangeTrackingOperationsTests(ITestOutputHelper output)
    {
        _output = output;
        _connectionString = TestConfiguration.GetConnectionString();
    }

    public async Task InitializeAsync()
    {
        _dataSource = SqlClientFactory.Instance.CreateDataSource(_connectionString);
        _dataSourceLowPrivilage = SqlClientFactory.Instance.CreateDataSource(TestConfiguration.GetLowPrivilageConnectionString());
        _operations = new SqlServerChangeTrackingOperations("test-source", _dataSource);
        _lowPrivilageOperations = new SqlServerChangeTrackingOperations("test-source-low-privilage", _dataSourceLowPrivilage);

        // Ensure change tracking is enabled at database level
        await EnableDatabaseChangeTracking();

        // Create test tables
        await CreateTestTables();
    }

    public async Task DisposeAsync()
    {
        // Clean up test tables
        await DropTestTables();

        await _dataSource.DisposeAsync();
        _operations.Dispose();
    }

    private async Task EnableDatabaseChangeTracking()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkQuery = "SELECT is_auto_cleanup_on, retention_period, retention_period_units FROM sys.change_tracking_databases WHERE database_id = DB_ID()";
        using var checkCommand = new SqlCommand(checkQuery, connection);
        using var reader = await checkCommand.ExecuteReaderAsync();

        if (!reader.HasRows)
        {
            await reader.CloseAsync();
            var enableQuery = "ALTER DATABASE CURRENT SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON)";
            using var enableCommand = new SqlCommand(enableQuery, connection);
            await enableCommand.ExecuteNonQueryAsync();
            _output.WriteLine("Enabled change tracking at database level");
        }
        else
        {
            await reader.CloseAsync();
        }
    }

    private async Task CreateTestTables()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Create first test table
        var createTable1 = $@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{_testTableName}')
                BEGIN
                    CREATE TABLE [{_testTableName}] (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Name NVARCHAR(100) NOT NULL,
                        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
                    )
                END";

        using var command1 = new SqlCommand(createTable1, connection);
        await command1.ExecuteNonQueryAsync();

        // Create second test table
        var createTable2 = $@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{_testTableName2}')
                BEGIN
                    CREATE TABLE [{_testTableName2}] (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Value INT NOT NULL,
                        Description NVARCHAR(200)
                    )
                END";

        using var command2 = new SqlCommand(createTable2, connection);
        await command2.ExecuteNonQueryAsync();

        _output.WriteLine($"Created test tables: {_testTableName}, {_testTableName2}");
    }

    private async Task DropTestTables()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Disable change tracking first (if enabled)
        var disableTracking1 = $"ALTER TABLE [{_testTableName}] DISABLE CHANGE_TRACKING";
        var disableTracking2 = $"ALTER TABLE [{_testTableName2}] DISABLE CHANGE_TRACKING";

        try
        {
            using var cmd1 = new SqlCommand(disableTracking1, connection);
            await cmd1.ExecuteNonQueryAsync();
        }
        catch { /* Ignore if not enabled */ }

        try
        {
            using var cmd2 = new SqlCommand(disableTracking2, connection);
            await cmd2.ExecuteNonQueryAsync();
        }
        catch { /* Ignore if not enabled */ }

        // Drop tables
        var dropTable1 = $"DROP TABLE IF EXISTS [{_testTableName}]";
        var dropTable2 = $"DROP TABLE IF EXISTS [{_testTableName2}]";

        using var cmd3 = new SqlCommand(dropTable1, connection);
        await cmd3.ExecuteNonQueryAsync();

        using var cmd4 = new SqlCommand(dropTable2, connection);
        await cmd4.ExecuteNonQueryAsync();

        _output.WriteLine($"Dropped test tables: {_testTableName}, {_testTableName2}");
    }

    [Fact]
    public void Constructor_WithDataSource_InitializesCorrectly()
    {
        // Arrange & Act
        var operations = new SqlServerChangeTrackingOperations("test-source", _dataSource);

        // Assert
        Assert.NotNull(operations);
        Assert.Equal("test-source", operations.SourceId);

        operations.Dispose();
    }

    [Fact]
    public void Constructor_WithConnectionString_InitializesCorrectly()
    {
        // Arrange & Act
        var operations = new SqlServerChangeTrackingOperations("test-source", _connectionString);

        // Assert
        Assert.NotNull(operations);
        Assert.Equal("test-source", operations.SourceId);

        operations.Dispose();
    }

    [Fact]
    public void Constructor_WithNullSourceId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SqlServerChangeTrackingOperations(null!, _dataSource));
        Assert.Throws<ArgumentException>(() => new SqlServerChangeTrackingOperations("", _connectionString));
    }

    [Fact]
    public void Constructor_WithNullDataSource_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SqlServerChangeTrackingOperations("test", (DbDataSource)null!));
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SqlServerChangeTrackingOperations("test", (string)null!));
        Assert.Throws<ArgumentException>(() => new SqlServerChangeTrackingOperations("test", ""));
    }

    [Fact]
    public void SourceId_ReturnsCorrectValue()
    {
        // Arrange & Act & Assert
        Assert.Equal("test-source", _operations.SourceId);
    }

    [Fact]
    public async Task GetLastTimestamp_WithoutKey_ReturnsDatabaseVersion()
    {
        // Arrange
        // Make some changes to increment the version
        await MakeSomeChanges();

        // Act
        var timestamp = await _operations.GetLastVersion(CancellationToken.None);

        // Assert
        Assert.True(timestamp > DateTimeOffset.MinValue.Ticks);
        Assert.True(timestamp <= DateTimeOffset.UtcNow.Ticks);
        _output.WriteLine($"Database timestamp: {timestamp}");
    }

    [Fact]
    public async Task GetLastTimestamp_ForNonExistentTable_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentTable = $"NonExistentTable_{Guid.NewGuid():N}";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _operations.GetLastVersion(nonExistentTable, CancellationToken.None));
    }

    [Fact]
    public async Task GetLastTimestamp_ForTableWithoutTracking_ThrowsInvalidOperationException()
    {
        // Arrange
        // Table exists but change tracking is not enabled

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _operations.GetLastVersion(_testTableName, CancellationToken.None));
    }

    [Fact]
    public async Task EnableTracking_ForTable_EnablesSuccessfully()
    {
        // Arrange

        // Act
        var result = await _operations.EnableTracking(_testTableName, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify tracking is enabled
        var isTracking = await _operations.IsTracking(_testTableName, CancellationToken.None);
        Assert.True(isTracking);

        _output.WriteLine($"Enabled change tracking for table: {_testTableName}");
    }

    [Fact]
    public async Task IsTracking_ForEnabledTable_ReturnsTrue()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName, CancellationToken.None);

        // Act
        var isTracking = await _operations.IsTracking(_testTableName, CancellationToken.None);

        // Assert
        Assert.True(isTracking);
    }

    [Fact]
    public async Task IsTracking_ForDisabledTable_ReturnsFalse()
    {
        // Arrange
        // Table exists but tracking is not enabled

        // Act
        var isTracking = await _operations.IsTracking(_testTableName, CancellationToken.None);

        // Assert
        Assert.False(isTracking);
    }

    [Fact]
    public async Task DisableTracking_ForEnabledTable_DisablesSuccessfully()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName, CancellationToken.None);

        // Act
        var result = await _operations.DisableTracking(_testTableName, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify tracking is disabled
        var isTracking = await _operations.IsTracking(_testTableName, CancellationToken.None);
        Assert.False(isTracking);

        _output.WriteLine($"Disabled change tracking for table: {_testTableName}");
    }

    [Fact]
    public async Task DisableTracking_WhenTableExistsAndChangeTrackingEnabled_ReturnsTrue()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);

        // Act
        var result = await _operations.DisableTracking(_testTableName);

        // Assert
        Assert.True(result);
        Assert.False(await _operations.IsTracking(_testTableName));
    }

    [Fact]
    public async Task DisableTracking_WhenTableExistsButChangeTrackingNotEnabled_ReturnsFalse()
    {
        // Arrange - Ensure change tracking is NOT enabled for the table
        await _operations.DisableTracking(_testTableName);

        // Act
        var result = await _operations.DisableTracking(_testTableName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DisableTracking_WhenTableDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentTable = "NonExistentTable_" + Guid.NewGuid().ToString("N");

        //Act
        var result = await _operations.DisableTracking(nonExistentTable);

        // Act & Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DisableTracking_WithEmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var emptyTableName = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _operations.DisableTracking(emptyTableName));
    }

    [Fact]
    public async Task DisableTracking_WithNullTableName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _operations.DisableTracking(null));
    }

    [Fact]
    public async Task DisableTracking_WhenTableHasSchemaPrefix_WorksCorrectly()
    {
        // Arrange
        var schemaTableName = "dbo." + _testTableName2;
        await _operations.EnableTracking(_testTableName2);

        // Act
        var result = await _operations.DisableTracking(schemaTableName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DisableTracking_WhenCalledAfterTableDropped_ThrowsException()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);

        // Drop table while change tracking is enabled
        await DropTestTables();

        // Recreate table WITHOUT change tracking
        await CreateTestTables();

        // Act & Assert - Should throw because table doesn't have change tracking
        var result = await _operations.DisableTracking(_testTableName);
        Assert.False(result);
    }

    [Fact]
    public async Task DisableTracking_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _operations.DisableTracking(_testTableName, cts.Token));
    }

    [Fact]
    public async Task DisableTracking_WhenUserLacksAlterPermission_ReturnFalse()
    {
        await _operations.EnableTracking(_testTableName);

        var result = await _lowPrivilageOperations.DisableTracking(_testTableName);

        Assert.False(result);
    }

    [Fact]
    public async Task DisableTracking_VerifyNoSideEffectsOnOtherTables()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);
        await _operations.EnableTracking(_testTableName2);

        // Act - Disable tracking on first table only
        var result = await _operations.DisableTracking(_testTableName);

        // Assert
        Assert.True(result);
        Assert.False(await _operations.IsTracking(_testTableName));
        Assert.True(await _operations.IsTracking(_testTableName2));
    }

    [Fact]
    public async Task DisableTracking_ForAlreadyDisabledTable_ReturnsFalse()
    {
        // Arrange
        // Table exists but tracking is not enabled

        // Act
        var result = await _operations.DisableTracking(_testTableName, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetLastTimestamp_ForEnabledTable_ReturnsTimestamp()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName, CancellationToken.None);

        var timestamp1 = await _operations.GetLastVersion(_testTableName, CancellationToken.None);
        _output.WriteLine($"Initial timestamp: {timestamp1}");

        await MakeChangesToTable(_testTableName);

        // Act
        var timestamp2 = await _operations.GetLastVersion(_testTableName, CancellationToken.None);
        _output.WriteLine($"After changes timestamp: {timestamp2}");

        // Assert
        Assert.True(timestamp2 > DateTimeOffset.MinValue.Ticks);
    }

    [Fact]
    public async Task GetLastTimestamps_ForMultipleTables_ReturnsAllTimestamps()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName, CancellationToken.None);
        await _operations.EnableTracking(_testTableName2, CancellationToken.None);

        // Make changes to both tables
        await MakeChangesToTable(_testTableName);
        await MakeChangesToTable2(_testTableName2);

        var keys = ImmutableArray.Create(_testTableName, _testTableName2);
        var versions = new long[keys.Length];

        // Act
        await _operations.GetLastVersions(keys, versions, CancellationToken.None);

        // Assert
        Assert.Equal(2, versions.Length);
        Assert.True(versions[0] > DateTimeOffset.MinValue.Ticks);
        Assert.True(versions[1] > DateTimeOffset.MinValue.Ticks);

        _output.WriteLine($"Table {_testTableName} timestamp: {versions[0]}");
        _output.WriteLine($"Table {_testTableName2} timestamp: {versions[1]}");
    }

    [Fact]
    public async Task GetLastVersions_WithSmallArray_ThrowsArgumentException()
    {
        // Arrange
        var keys = ImmutableArray.Create(_testTableName, _testTableName2);
        var versions = new long[1]; // Smaller than keys count

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _operations.GetLastVersions(keys, versions, CancellationToken.None));
    }

    [Fact]
    public async Task SetLastTimestamp_AlwaysThrowsInvalidOperationException()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName, CancellationToken.None);
        var timestamp = DateTimeOffset.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _operations.SetLastVersion(_testTableName, timestamp.Ticks, CancellationToken.None));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var operations = new SqlServerChangeTrackingOperations("dispose-test", _dataSource);

        // Act
        operations.Dispose();
        operations.Dispose(); // Should not throw

        // Assert
        // If we get here without exception, test passes
    }

    [Fact]
    public async Task MultipleOperations_CanWorkIndependently()
    {
        // Arrange
        var operations1 = new SqlServerChangeTrackingOperations("source1", _connectionString);
        var operations2 = new SqlServerChangeTrackingOperations("source2", _connectionString);

        try
        {
            // Act & Assert
            await operations1.EnableTracking(_testTableName, CancellationToken.None);
            await operations2.EnableTracking(_testTableName2, CancellationToken.None);

            var isTracking1 = await operations1.IsTracking(_testTableName, CancellationToken.None);
            var isTracking2 = await operations2.IsTracking(_testTableName2, CancellationToken.None);

            Assert.True(isTracking1);
            Assert.True(isTracking2);

            await MakeChangesToTable(_testTableName);
            await MakeChangesToTable2(_testTableName2);

            // Both should be able to get timestamps
            var timestamp1 = await operations1.GetLastVersion(_testTableName, CancellationToken.None);
            var timestamp2 = await operations2.GetLastVersion(_testTableName2, CancellationToken.None);

            Assert.True(timestamp1 > DateTimeOffset.MinValue.Ticks);
            Assert.True(timestamp2 > DateTimeOffset.MinValue.Ticks);
        }
        finally
        {
            operations1.Dispose();
            operations2.Dispose();
        }
    }

    private async Task MakeSomeChanges()
    {
        // Create a temporary table and make changes to increment the database version
        var tempTableName = $"TempTable_{Guid.NewGuid():N}";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Create and enable tracking on temp table
        var createTable = $"CREATE TABLE [{tempTableName}] (Id INT PRIMARY KEY)";
        using var createCommand = new SqlCommand(createTable, connection);
        await createCommand.ExecuteNonQueryAsync();

        var enableTracking = $"ALTER TABLE [{tempTableName}] ENABLE CHANGE_TRACKING";
        using var enableCommand = new SqlCommand(enableTracking, connection);
        await enableCommand.ExecuteNonQueryAsync();

        // Make some changes
        for (int i = 0; i < 3; i++)
        {
            var insertQuery = $"INSERT INTO [{tempTableName}] (Id) VALUES ({i})";
            using var insertCommand = new SqlCommand(insertQuery, connection);
            await insertCommand.ExecuteNonQueryAsync();
        }

        // Clean up
        var disableTracking = $"ALTER TABLE [{tempTableName}] DISABLE CHANGE_TRACKING";
        using var disableCommand = new SqlCommand(disableTracking, connection);
        await disableCommand.ExecuteNonQueryAsync();

        var dropTable = $"DROP TABLE [{tempTableName}]";
        using var dropCommand = new SqlCommand(dropTable, connection);
        await dropCommand.ExecuteNonQueryAsync();
    }

    private async Task MakeChangesToTable(string tableName)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Insert some data
        var insertQuery = $"INSERT INTO [{tableName}] (Name) VALUES ('TestChange_{Guid.NewGuid()}')";
        using var insertCommand = new SqlCommand(insertQuery, connection);
        await insertCommand.ExecuteNonQueryAsync();

        _output.WriteLine($"Made change to table {tableName}");
    }

    private async Task MakeChangesToTable2(string tableName)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Insert some data
        var insertQuery = $"INSERT INTO [{tableName}] (Value) VALUES (1)";
        using var insertCommand = new SqlCommand(insertQuery, connection);
        await insertCommand.ExecuteNonQueryAsync();

        _output.WriteLine($"Made change to table {tableName}");
    }
}

public static class TestConfiguration
{
    public static string GetConnectionString()
    {
        return "Data Source=localhost,1433;User ID=sa;Password=Password1;Database=TrackerTestDb;TrustServerCertificate=True;";
    }

    public static string GetLowPrivilageConnectionString()
    {
        return "Data Source=localhost,1433;User ID=lowprivilege;Password=Password1;Database=TrackerTestDb;TrustServerCertificate=True;";
    }
}
