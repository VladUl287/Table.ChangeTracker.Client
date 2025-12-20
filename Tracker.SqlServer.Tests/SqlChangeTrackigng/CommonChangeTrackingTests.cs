using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tracker.SqlServer.Services;
using Tracker.SqlServer.Tests.Utils;

namespace Tracker.SqlServer.Tests.SqlChangeTrackigng;

public class CommonChangeTrackingTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly DbDataSource _dataSource;
    private readonly SqlServerChangeTrackingOperations _operations;

    private readonly string _testTableName = $"TestTable_{Guid.NewGuid():N}";
    private readonly string _testTableName2 = $"TestTable_{Guid.NewGuid():N}";

    public CommonChangeTrackingTests()
    {
        _connectionString = TestConfiguration.GetConnectionString();
        _dataSource = SqlClientFactory.Instance.CreateDataSource(_connectionString);
        _operations = new SqlServerChangeTrackingOperations("test-source", _dataSource);
    }

    public async Task InitializeAsync()
    {
        await SqlServerHelpers.EnableDatabaseChangeTracking(_connectionString);
        await SqlServerHelpers.CreateTestTable(_connectionString, _testTableName);
        await SqlServerHelpers.CreateTestTable(_connectionString, _testTableName2);
    }

    public async Task DisposeAsync()
    {
        await SqlServerHelpers.DropTable(_connectionString, _testTableName);
        await SqlServerHelpers.DropTable(_connectionString, _testTableName2);
        await SqlServerHelpers.DisableChangeTrackingForAllTables(_connectionString);

        await _dataSource.DisposeAsync();
        _operations.Dispose();
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

            await SqlServerHelpers.InsertToTestTable(_connectionString, _testTableName, 1);
            await SqlServerHelpers.InsertToTestTable(_connectionString, _testTableName2, 1);

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
}