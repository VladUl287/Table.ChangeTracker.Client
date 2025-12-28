using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tracker.AspNet.Middlewares;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Tests.ExtensionsTests;

public class ApplicationBuilderExtensionsTests
{
    private readonly Mock<IApplicationBuilder> _mockApplicationBuilder;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;

    public ApplicationBuilderExtensionsTests()
    {
        _mockApplicationBuilder = new Mock<IApplicationBuilder>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        _mockApplicationBuilder.SetupGet(x => x.ApplicationServices)
            .Returns(_mockServiceProvider.Object);
    }

    [Fact]
    public void UseTracker_WithoutParameters_ReturnsBuilderWithMiddleware()
    {
        // Arrange
        var builder = _mockApplicationBuilder;

        builder.Setup(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(_mockApplicationBuilder.Object);

        // Act
        var result = Extensions.ApplicationBuilderExtensions.UseTracker(builder.Object);

        // Assert
        Assert.Same(builder.Object, result);
        _mockApplicationBuilder.Verify(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
        _mockApplicationBuilder.VerifyNoOtherCalls();
    }

    [Fact]
    public void UseTracker_WithOptions_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        GlobalOptions options = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Tracker.AspNet.Extensions.ApplicationBuilderExtensions.UseTracker(builder, options));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void UseTracker_WithOptions_WhenOptionsBuilderServiceIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        var options = new GlobalOptions();

        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>)))
            .Returns(null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            Tracker.AspNet.Extensions.ApplicationBuilderExtensions.UseTracker(builder, options));
    }

    [Fact]
    public void UseTracker_WithOptions_BuildsImmutableOptionsAndReturnsBuilderWithMiddleware()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        var options = new GlobalOptions();
        var immutableOptions = new ImmutableGlobalOptions();

        _mockApplicationBuilder.Setup(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(_mockApplicationBuilder.Object);

        var mockOptionsBuilder = new Mock<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
        mockOptionsBuilder.Setup(x => x.Build(options))
            .Returns(immutableOptions);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>)))
            .Returns(mockOptionsBuilder.Object);

        // Act
        var result = Extensions.ApplicationBuilderExtensions.UseTracker(builder, options);

        // Assert
        Assert.Same(builder, result);
        mockOptionsBuilder.Verify(x => x.Build(options), Times.Once);
        _mockApplicationBuilder.Verify(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }

    [Fact]
    public void UseTracker_GenericWithOptions_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        GlobalOptions options = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Tracker.AspNet.Extensions.ApplicationBuilderExtensions.UseTracker<TestDbContext>(builder, options));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void UseTracker_GenericWithOptions_WhenOptionsBuilderServiceIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        var options = new GlobalOptions();

        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>)))
            .Returns(null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            Tracker.AspNet.Extensions.ApplicationBuilderExtensions.UseTracker<TestDbContext>(builder, options));
    }

    [Fact]
    public void UseTracker_GenericWithOptions_BuildsImmutableOptionsWithContextAndReturnsBuilderWithMiddleware()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        var options = new GlobalOptions();
        var immutableOptions = new ImmutableGlobalOptions();

        _mockApplicationBuilder.Setup(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(_mockApplicationBuilder.Object);

        var mockOptionsBuilder = new Mock<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
        mockOptionsBuilder.Setup(x => x.Build<TestDbContext>(options))
            .Returns(immutableOptions);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>)))
            .Returns(mockOptionsBuilder.Object);

        // Act
        var result = Extensions.ApplicationBuilderExtensions.UseTracker<TestDbContext>(builder, options);

        // Assert
        Assert.Same(builder, result);
        mockOptionsBuilder.Verify(x => x.Build<TestDbContext>(options), Times.Once);
        _mockApplicationBuilder.Verify(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }

    [Fact]
    public void UseTracker_GenericWithConfigureAction_WhenConfigureIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        Action<GlobalOptions> configure = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Tracker.AspNet.Extensions.ApplicationBuilderExtensions.UseTracker<TestDbContext>(builder, configure));

        Assert.Equal("configure", exception.ParamName);
    }

    [Fact]
    public void UseTracker_GenericWithConfigureAction_CreatesOptionsConfiguresThemAndCallsGenericOverload()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        var configuredProperty = "TestValue";
        var capturedOptions = (GlobalOptions)null;

        _mockApplicationBuilder.Setup(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(_mockApplicationBuilder.Object);

        var mockOptionsBuilder = new Mock<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
        mockOptionsBuilder.Setup(x => x.Build<TestDbContext>(It.IsAny<GlobalOptions>()))
            .Callback<GlobalOptions>(opts => capturedOptions = opts)
            .Returns(new ImmutableGlobalOptions());

        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>)))
            .Returns(mockOptionsBuilder.Object);

        // Act
        var result = Extensions.ApplicationBuilderExtensions.UseTracker<TestDbContext>(builder, options =>
        {
            options.ProviderId = configuredProperty;
        });

        // Assert
        Assert.Same(builder, result);
        Assert.NotNull(capturedOptions);
        Assert.Equal(configuredProperty, capturedOptions.ProviderId);
        _mockApplicationBuilder.Verify(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }

    [Fact]
    public void UseTracker_WithConfigureAction_WhenConfigureIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        Action<GlobalOptions> configure = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Extensions.ApplicationBuilderExtensions.UseTracker(builder, configure));

        Assert.Equal("configure", exception.ParamName);
    }

    [Fact]
    public void UseTracker_WithConfigureAction_CreatesOptionsConfiguresThemAndCallsOverloadWithOptions()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        var configuredProperty = "TestValue";
        var capturedOptions = (GlobalOptions)null;

        _mockApplicationBuilder.Setup(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
           .Returns(_mockApplicationBuilder.Object);

        var mockOptionsBuilder = new Mock<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
        mockOptionsBuilder.Setup(x => x.Build(It.IsAny<GlobalOptions>()))
            .Callback<GlobalOptions>(opts => capturedOptions = opts)
            .Returns(new ImmutableGlobalOptions());

        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>)))
            .Returns(mockOptionsBuilder.Object);

        // Act
        var result = Extensions.ApplicationBuilderExtensions.UseTracker(builder, options =>
        {
            options.ProviderId = configuredProperty;
        });

        // Assert
        Assert.Same(builder, result);
        Assert.NotNull(capturedOptions);
        Assert.Equal(configuredProperty, capturedOptions.ProviderId);
        _mockApplicationBuilder.Verify(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }

    [Fact]
    public void UseTracker_GenericWithConfigureAction_UsesSameOptionsBuilderService()
    {
        // Arrange
        var builder = _mockApplicationBuilder.Object;
        var mockOptionsBuilder = new Mock<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
        var immutableOptions = new ImmutableGlobalOptions();

        _mockApplicationBuilder.Setup(c => c.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
           .Returns(_mockApplicationBuilder.Object);

        mockOptionsBuilder.Setup(x => x.Build<TestDbContext>(It.IsAny<GlobalOptions>()))
            .Returns(immutableOptions);

        _mockServiceProvider.SetupSequence(x => x.GetService(typeof(IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>)))
            .Returns(mockOptionsBuilder.Object);

        // Act
        var result = Extensions.ApplicationBuilderExtensions.UseTracker<TestDbContext>(builder, options => { });

        // Assert
        Assert.Same(builder, result);
        _mockServiceProvider.Verify(x => x.GetService(typeof(IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>)), Times.Once);
    }

    private class TestDbContext : DbContext
    {}
}