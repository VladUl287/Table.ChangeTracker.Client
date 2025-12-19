using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Tracker.Core.Services.Contracts;
using Tracker.Npgsql.Extensions;
using Tracker.Npgsql.Services;

namespace Tracker.Npgsql.Tests;

public class ServiceCollectionExtensionsTests
{
    #region AddNpgsqlSource<TContext> - with ISourceIdGenerator

    [Fact]
    public void AddNpgsqlSource_WithDbContextType_RegistersISourceOperationsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        SetupMockDbContext(services, "Host=localhost;Database=test");

        services.AddScoped<ISourceIdGenerator>(_ => Mock.Of<ISourceIdGenerator>(g =>
            g.GenerateId<TestDbContext>() == "generated-source-id"));

        // Act
        services.AddNpgsqlSource<TestDbContext>();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISourceOperations));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddNpgsqlSource_WithDbContextType_ResolvesWithGeneratedSourceId()
    {
        // Arrange
        var services = new ServiceCollection();
        const string expectedConnectionString = "Host=localhost;Database=test";
        const string expectedSourceId = "test-context-generated-id";

        SetupMockDbContext(services, expectedConnectionString);

        var mockIdGenerator = new Mock<ISourceIdGenerator>();
        mockIdGenerator.Setup(g => g.GenerateId<TestDbContext>()).Returns(expectedSourceId);
        services.AddScoped(_ => mockIdGenerator.Object);

        // Act
        services.AddNpgsqlSource<TestDbContext>();
        var provider = services.BuildServiceProvider();
        var operations = provider.GetRequiredService<ISourceOperations>();

        // Assert
        Assert.NotNull(operations);
        Assert.IsType<NpgsqlOperations>(operations);
    }

    [Fact]
    public void AddNpgsqlSource_WithDbContextType_ThrowsWhenConnectionStringIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        SetupMockDbContext(services, (string)null);

        services.AddScoped<ISourceIdGenerator>(_ => Mock.Of<ISourceIdGenerator>());

        // Act & Assert
        services.AddNpgsqlSource<TestDbContext>();
        var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<NullReferenceException>(() =>
            provider.GetRequiredService<ISourceOperations>());

        Assert.Contains(typeof(TestDbContext).FullName, exception.Message);
    }

    #endregion

    #region AddNpgsqlSource<TContext> - with explicit sourceId

    [Fact]
    public void AddNpgsqlSource_WithDbContextAndSourceId_RegistersISourceOperationsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        SetupMockDbContext(services, "Host=localhost;Database=test");

        // Act
        services.AddNpgsqlSource<TestDbContext>("custom-source-id");

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISourceOperations));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddNpgsqlSource_WithDbContextAndSourceId_ThrowsWhenSourceIdIsNullOrEmpty()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddNpgsqlSource<TestDbContext>(null));
        Assert.Throws<ArgumentException>(() => services.AddNpgsqlSource<TestDbContext>(""));
    }

    [Fact]
    public void AddNpgsqlSource_WithDbContextAndSourceId_UsesProvidedSourceId()
    {
        // Arrange
        var services = new ServiceCollection();
        const string expectedConnectionString = "Host=localhost;Database=test";
        const string expectedSourceId = "custom-source-id";

        SetupMockDbContext(services, expectedConnectionString);

        // Act
        services.AddNpgsqlSource<TestDbContext>(expectedSourceId);
        var provider = services.BuildServiceProvider();
        var operations = provider.GetRequiredService<ISourceOperations>();

        // Assert
        Assert.NotNull(operations);
        Assert.IsType<NpgsqlOperations>(operations);
    }

    #endregion

    #region AddNpgsqlSource - with sourceId and connectionString

    [Fact]
    public void AddNpgsqlSource_WithSourceIdAndConnectionString_RegistersISourceOperationsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpgsqlSource("test-id", "Host=localhost;Database=test");

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISourceOperations));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddNpgsqlSource_WithSourceIdAndConnectionString_ThrowsWhenArgumentsInvalid()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddNpgsqlSource(null, "connection-string"));
        Assert.Throws<ArgumentException>(() => services.AddNpgsqlSource("", "connection-string"));

        Assert.Throws<ArgumentNullException>(() => services.AddNpgsqlSource("source-id", (string)null));
        Assert.Throws<ArgumentException>(() => services.AddNpgsqlSource("source-id", ""));
    }

    [Fact]
    public void AddNpgsqlSource_WithSourceIdAndConnectionString_CreatesOperationsDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        const string expectedSourceId = "test-id";
        const string expectedConnectionString = "Host=localhost;Database=test;ApplicationName=Test";

        // Act
        services.AddNpgsqlSource(expectedSourceId, expectedConnectionString);
        var provider = services.BuildServiceProvider();
        var operations = provider.GetRequiredService<ISourceOperations>();

        // Assert
        Assert.NotNull(operations);
        var npgsqlOps = Assert.IsType<NpgsqlOperations>(operations);
    }

    #endregion

    #region AddNpgsqlSource - with sourceId and configure action

    [Fact]
    public void AddNpgsqlSource_WithSourceIdAndConfigureAction_RegistersISourceOperationsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpgsqlSource("test-id", builder =>
            builder.ConnectionStringBuilder.ApplicationName = "TestApp");

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISourceOperations));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddNpgsqlSource_WithSourceIdAndConfigureAction_ThrowsWhenArgumentsInvalid()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddNpgsqlSource(null, builder => { }));
        Assert.Throws<ArgumentException>(() =>
            services.AddNpgsqlSource("", builder => { }));

        Assert.Throws<ArgumentNullException>(() =>
            services.AddNpgsqlSource("source-id", (Action<NpgsqlDataSourceBuilder>)null));
    }

    [Fact]
    public void AddNpgsqlSource_WithSourceIdAndConfigureAction_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        const string expectedSourceId = "test-id";
        const string expectedApplicationName = "TestApplication";
        const string expectedHost = "localhost";
        bool wasConfigured = false;

        // Act
        services.AddNpgsqlSource(expectedSourceId, builder =>
        {
            builder.ConnectionStringBuilder.Host = expectedHost;
            builder.ConnectionStringBuilder.ApplicationName = expectedApplicationName;
            wasConfigured = true;
        });

        var provider = services.BuildServiceProvider();
        var operations = provider.GetRequiredService<ISourceOperations>();

        // Assert
        Assert.True(wasConfigured);
        Assert.NotNull(operations);
        Assert.IsType<NpgsqlOperations>(operations);
    }

    [Fact]
    public void AddNpgsqlSource_WithSourceIdAndConfigureAction_CreatesDataSourceFromBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        const string expectedSourceId = "test-id";
        const string expectedDatabase = "testdb";

        // Act
        services.AddNpgsqlSource(expectedSourceId, builder =>
        {
            builder.ConnectionStringBuilder.Host = "localhost";
            builder.ConnectionStringBuilder.Database = expectedDatabase;
        });

        var provider = services.BuildServiceProvider();
        var operations = provider.GetRequiredService<ISourceOperations>();

        // Assert
        Assert.NotNull(operations);
        Assert.IsType<NpgsqlOperations>(operations);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void MultipleRegistrations_LastOneWins()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpgsqlSource("first-id", "Host=l;Port=5432;Database=1;Username=1;Password=1");
        services.AddNpgsqlSource("second-id", "Host=l;Port=5432;Database=2;Username=2;Password=2");

        var provider = services.BuildServiceProvider();
        var operations = provider.GetRequiredService<ISourceOperations>();

        // Assert
        Assert.NotNull(operations);
    }

    [Fact]
    public void AllMethods_CreateValidNpgsqlOperationsInstance()
    {
        // Test each overload creates a valid instance

        // Method 1: With DbContext and ISourceIdGenerator
        var services1 = new ServiceCollection();
        SetupMockDbContext(services1, "Host=l;Port=5432;Database=1;Username=1;Password=1");
        services1.AddScoped<ISourceIdGenerator>(_ =>
            Mock.Of<ISourceIdGenerator>(g => g.GenerateId<TestDbContext>() == "id1"));
        services1.AddNpgsqlSource<TestDbContext>();

        var provider1 = services1.BuildServiceProvider();
        var ops1 = provider1.GetRequiredService<ISourceOperations>();
        Assert.IsType<NpgsqlOperations>(ops1);

        // Method 2: With DbContext and explicit sourceId
        var services2 = new ServiceCollection();
        SetupMockDbContext(services2, "Host=l;Port=5432;Database=2;Username=2;Password=2");
        services2.AddNpgsqlSource<TestDbContext>("id2");

        var provider2 = services2.BuildServiceProvider();
        var ops2 = provider2.GetRequiredService<ISourceOperations>();
        Assert.IsType<NpgsqlOperations>(ops2);

        // Method 3: With sourceId and connectionString
        var services3 = new ServiceCollection();
        services3.AddNpgsqlSource("id3", "Host=l;Port=5432;Database=3;Username=3;Password=3");

        var provider3 = services3.BuildServiceProvider();
        var ops3 = provider3.GetRequiredService<ISourceOperations>();
        Assert.IsType<NpgsqlOperations>(ops3);

        // Method 4: With sourceId and configure action
        var services4 = new ServiceCollection();
        services4.AddNpgsqlSource("id4", builder =>
            builder.ConnectionStringBuilder.ConnectionString = "Host=localhost");

        var provider4 = services4.BuildServiceProvider();
        var ops4 = provider4.GetRequiredService<ISourceOperations>();
        Assert.IsType<NpgsqlOperations>(ops4);
    }

    #endregion

    #region Helper Methods

    private static void SetupMockDbContext(IServiceCollection services, string connectionString)
    {
        var mockRelationalConnection = new Mock<IRelationalConnection>();
        mockRelationalConnection.SetupGet(c => c.ConnectionString)
            .Returns(connectionString);

        var mockRelationalDatabaseFacadeDependencies = new Mock<IRelationalDatabaseFacadeDependencies>();
        mockRelationalDatabaseFacadeDependencies.SetupGet(d => d.RelationalConnection)
            .Returns(mockRelationalConnection.Object);

        var mockDbContext = new Mock<TestDbContext>();
        var databaseFacadeAccessor = new Mock<DatabaseFacade>(mockDbContext.Object);
        var mockDatabaseFacadeDependenciesAccessor = databaseFacadeAccessor.As<IDatabaseFacadeDependenciesAccessor>()
            .Setup(c => c.Dependencies)
            .Returns(mockRelationalDatabaseFacadeDependencies.Object);

        mockDbContext.Setup(c => c.Database)
            .Returns(databaseFacadeAccessor.Object);

        services.AddScoped(_ => mockDbContext.Object);
    }

    #endregion
}

public class TestDbContext : DbContext { }