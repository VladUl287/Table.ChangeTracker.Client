using Microsoft.Data.SqlClient;
using System.Data.Common;
using Tracker.SqlServer.Services;
using Tracker.SqlServer.Tests.Utils;

namespace Tracker.SqlServer.Tests.SqlChangeTrackigng;

public class EnableTrackingTests : IAsyncLifetime
{
    private readonly string connectionString;
    private readonly string _lowPrivilageConnectionString;
    private readonly DbDataSource _dataSource;
    private readonly DbDataSource _lowPrivilageDataSource;
    private readonly SqlServerChangeTrackingOperations _operations;
    private readonly SqlServerChangeTrackingOperations _lowPrivilagesOperations;

    private readonly string _testTableName = $"TestTable_{Guid.NewGuid():N}";
    private readonly string _testTableName2 = $"TestTable_{Guid.NewGuid():N}";

    public EnableTrackingTests()
    {
        connectionString = TestConfiguration.GetConnectionString();
        _lowPrivilageConnectionString = TestConfiguration.GetLowPrivilageConnectionString();
        _dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);
        _lowPrivilageDataSource = SqlClientFactory.Instance.CreateDataSource(_lowPrivilageConnectionString);
        _operations = new SqlServerChangeTrackingOperations("test-source", _dataSource);
        _lowPrivilagesOperations = new SqlServerChangeTrackingOperations("test-source-low-privilages", _lowPrivilageDataSource);
    }

    public async Task InitializeAsync()
    {
        await SqlServerHelpers.EnableDatabaseChangeTracking(connectionString);
        await SqlServerHelpers.CreateTestTable(connectionString, _testTableName);
        await SqlServerHelpers.CreateTestTable(connectionString, _testTableName2);
    }

    public async Task DisposeAsync()
    {
        await SqlServerHelpers.DropTable(connectionString, _testTableName);
        await SqlServerHelpers.DropTable(connectionString, _testTableName2);
        await SqlServerHelpers.DisableChangeTrackingForAllTables(connectionString);

        await _dataSource.DisposeAsync();
        _operations.Dispose();
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
    }

    [Fact]
    public async Task EnableTracking_WhenTableAlreadyHasChangeTrackingEnabled_ThrowsSqlException()
    {
        // Arrange - Already enabled
        await _operations.EnableTracking(_testTableName);

        // Act
        // Assert - Returns true but was already enabled
        await Assert.ThrowsAsync<SqlException>(async () => await _operations.EnableTracking(_testTableName));
        Assert.True(await _operations.IsTracking(_testTableName));
    }

    [Fact]
    public async Task EnableTracking_WithEmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var emptyTableName = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _operations.EnableTracking(emptyTableName));
    }

    [Fact]
    public async Task EnableTracking_WithNullTableName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _operations.EnableTracking(null));
    }

    [Fact]
    public async Task EnableTracking_WhenTableExistsAndChangeTrackingNotEnabled_ReturnsTrue()
    {
        // Arrange - Ensure change tracking is NOT enabled
        try
        {
            await _operations.DisableTracking(_testTableName);
        }
        catch { }

        // Act
        var result = await _operations.EnableTracking(_testTableName);

        // Assert
        Assert.True(result);
        Assert.True(await _operations.IsTracking(_testTableName));
    }

    [Fact]
    public async Task EnableTracking_WhenDatabaseChangeTrackingDisabled_ThrowsException()
    {
        // Arrange
        await SqlServerHelpers.DisableDatabaseChangeTracking(connectionString);

        try
        {
            // Act & Assert - Should fail because database-level change tracking is off
            await Assert.ThrowsAsync<SqlException>(async () =>
                await _operations.EnableTracking(_testTableName));
        }
        finally
        {
            await SqlServerHelpers.EnableDatabaseChangeTracking(connectionString);
        }
    }

    [Fact]
    public async Task EnableTracking_WhenTableHasSchemaPrefix_WorksCorrectly()
    {
        // Arrange
        var schemaTableName = "dbo." + _testTableName2;
        try { await _operations.DisableTracking(_testTableName2); }
        catch { }

        // Act
        var result = await _operations.EnableTracking(schemaTableName);

        // Assert
        Assert.True(result);
        Assert.True(await _operations.IsTracking(_testTableName2));
    }

    [Fact]
    public async Task EnableTracking_WhenTableHasPrimaryKey_WorksCorrectly()
    {
        // Arrange
        try { await _operations.DisableTracking(_testTableName2); }
        catch { }

        // Act
        var result = await _operations.EnableTracking(_testTableName);

        // Assert
        Assert.True(result);
        Assert.True(await _operations.IsTracking(_testTableName));
    }

    [Fact]
    public async Task EnableTracking_WhenTableHasNoPrimaryKey_ThrowsException()
    {
        // Arrange
        var tableNoPk = $"TableNoPk_{Guid.NewGuid():N}";

        await SqlServerHelpers.CreateTestTableWithNoKey(connectionString, tableNoPk);

        try
        {
            // Act & Assert - Should fail because change tracking requires primary key
            await Assert.ThrowsAsync<SqlException>(async () =>
                await _operations.EnableTracking(tableNoPk));
        }
        finally
        {
            await SqlServerHelpers.DropTable(connectionString, tableNoPk);
        }
    }

    [Fact]
    public async Task EnableTracking_WhenTableHasExistingData_TrackingStartsFromCurrentState()
    {
        // Arrange
        try { await _operations.DisableTracking(_testTableName); }
        catch { }

        await SqlServerHelpers.InsertToTestTable(connectionString, _testTableName, 1);

        // Act
        var result = await _operations.EnableTracking(_testTableName);

        // Assert
        Assert.True(result);

        // Verify tracking is enabled and we can get change version
        var versionQuery = $"SELECT CHANGE_TRACKING_CURRENT_VERSION()";
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        using var versionCmd = new SqlCommand(versionQuery, connection);
        var version = (long?)await versionCmd.ExecuteScalarAsync();
        Assert.NotNull(version);
    }

    [Fact]
    public async Task EnableTracking_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        try { await _operations.DisableTracking(_testTableName); }
        catch { }

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _operations.EnableTracking(_testTableName, cts.Token));
    }

    [Fact]
    public async Task EnableTracking_WhenUserLacksAlterPermission_ThrowsException()
    {
        // Arrange
        try { await _operations.DisableTracking(_testTableName); }
        catch { }

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SqlException>(async () =>
            await _lowPrivilagesOperations.EnableTracking(_testTableName));

        Assert.Contains("permission", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnableTracking_WhenTableIsSystemTable_ThrowsException()
    {
        // Arrange
        var systemTable = "sys.tables";

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () =>
            await _operations.EnableTracking(systemTable));
    }

    [Fact]
    public async Task EnableTracking_VerifyTRACK_COLUMNS_UPDATED_IsEnabled()
    {
        // Arrange
        try { await _operations.DisableTracking(_testTableName); }
        catch { }

        // Act
        var result = await _operations.EnableTracking(_testTableName);

        // Assert
        Assert.True(result);

        // Verify TRACK_COLUMNS_UPDATED is ON
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var checkQuery = @"
        SELECT is_track_columns_updated_on 
        FROM sys.change_tracking_tables 
        WHERE object_id = OBJECT_ID(@TableName)";

        using var cmd = new SqlCommand(checkQuery, connection);
        cmd.Parameters.AddWithValue("@TableName", _testTableName);

        var isTrackColumnsUpdated = (bool?)await cmd.ExecuteScalarAsync();
        Assert.True(isTrackColumnsUpdated.HasValue && isTrackColumnsUpdated.Value);
    }

    [Fact]
    public async Task EnableTracking_VerifyNoSideEffectsOnOtherTables()
    {
        // Arrange
        try
        {
            await _operations.DisableTracking(_testTableName);
            await _operations.DisableTracking(_testTableName2);
        }
        catch { }

        // Act - Enable tracking on first table only
        var result = await _operations.EnableTracking(_testTableName);

        // Assert
        Assert.True(result);
        Assert.True(await _operations.IsTracking(_testTableName));
        Assert.False(await _operations.IsTracking(_testTableName2)); // Should still be disabled
    }

    [Fact]
    public async Task EnableTracking_AfterDisableTracking_WorksCorrectly()
    {
        // Arrange - Enable, then disable, then enable again
        await _operations.EnableTracking(_testTableName);
        await _operations.DisableTracking(_testTableName);

        // Act - Enable again
        var result = await _operations.EnableTracking(_testTableName);

        // Assert
        Assert.True(result);
        Assert.True(await _operations.IsTracking(_testTableName));
    }

}
