using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Tests.ExtensionsTests;

public class EndpointBuilderExtensionsTests
{
    private readonly Mock<IEndpointConventionBuilder> _mockBuilder;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>> _mockOptionsBuilder;
    private readonly Mock<IRequestHandler> _mockEtagService;
    private readonly Mock<IRequestFilter> _mockRequestFilter;
    private readonly ServiceCollection _services;

    public EndpointBuilderExtensionsTests()
    {
        _mockBuilder = new Mock<IEndpointConventionBuilder>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockOptionsBuilder = new Mock<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
        _mockEtagService = new Mock<IRequestHandler>();
        _mockRequestFilter = new Mock<IRequestFilter>();

        _services = new ServiceCollection();

        SetupServiceProvider();
    }

    private void SetupServiceProvider()
    {
        _services.AddSingleton(_mockOptionsBuilder.Object);
        _services.AddSingleton(_mockEtagService.Object);
        _services.AddSingleton(_mockRequestFilter.Object);

        var serviceProvider = _services.BuildServiceProvider();

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);
    }

    [Fact]
    public void WithTracking_GenericTContext_WithOptions_ShouldAddEndpointFilterFactory()
    {
        // Arrange
        var options = new GlobalOptions();
        var immutableOptions = new ImmutableGlobalOptions();

        _mockOptionsBuilder
            .Setup(x => x.Build<DbContext>(options))
            .Returns(immutableOptions);

        // Act
        var result = _mockBuilder.Object.WithTracking<IEndpointConventionBuilder, DbContext>(options);

        // Assert
        Assert.Same(_mockBuilder.Object, result);
        _mockBuilder.Verify();
    }

    [Fact]
    public void WithTracking_GenericTContext_WithConfigureAction_ShouldCreateOptionsAndCallOverload()
    {
        // Arrange
        var configuredValue = "TestValue";
        var options = new GlobalOptions();
        var immutableOptions = new ImmutableGlobalOptions();

        _mockOptionsBuilder
            .Setup(x => x.Build<DbContext>(It.Is<GlobalOptions>(o => o.ProviderId == configuredValue)))
            .Returns(immutableOptions);

        // Act
        var result = _mockBuilder.Object.WithTracking<IEndpointConventionBuilder, DbContext>(opt =>
        {
            opt.ProviderId = configuredValue;
        });

        // Assert
        Assert.Same(_mockBuilder.Object, result);
        _mockOptionsBuilder.Verify(x => x.Build<DbContext>(It.Is<GlobalOptions>(o => o.ProviderId == configuredValue)), Times.Once);
    }

    [Fact]
    public void WithTracking_NonGeneric_ShouldAddTrackerEndpointFilter()
    {
        // Arrange
        _mockBuilder
            .Setup(x => x.AddEndpointFilter<IEndpointConventionBuilder, TrackerEndpointFilter>())
            .Returns(_mockBuilder.Object)
            .Verifiable();

        // Act
        var result = _mockBuilder.Object.WithTracking();

        // Assert
        Assert.Same(_mockBuilder.Object, result);
        _mockBuilder.Verify();
    }

    [Fact]
    public void WithTracking_NonGeneric_WithOptions_ShouldAddEndpointFilterFactory()
    {
        // Arrange
        var options = new GlobalOptions();
        var immutableOptions = new ImmutableGlobalOptions();

        _mockOptionsBuilder
            .Setup(x => x.Build(options))
            .Returns(immutableOptions);

        _mockBuilder
            .Setup(x => x.AddEndpointFilterFactory(It.IsAny<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()))
            .Returns(_mockBuilder.Object)
            .Verifiable();

        // Act
        var result = _mockBuilder.Object.WithTracking(options);

        // Assert
        Assert.Same(_mockBuilder.Object, result);
        _mockBuilder.Verify();
        _mockOptionsBuilder.Verify(x => x.Build(options), Times.Once);
    }

    [Fact]
    public void WithTracking_NonGeneric_WithConfigureAction_ShouldCreateOptionsAndCallOverload()
    {
        // Arrange
        var configuredValue = "TestValue";
        var immutableOptions = new ImmutableGlobalOptions();

        _mockOptionsBuilder
            .Setup(x => x.Build(It.Is<GlobalOptions>(o => o.ProviderId == configuredValue)))
            .Returns(immutableOptions);

        _mockBuilder
            .Setup(x => x.AddEndpointFilterFactory(It.IsAny<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()))
            .Returns(_mockBuilder.Object);

        // Act
        var result = _mockBuilder.Object.WithTracking(opt =>
        {
            opt.ProviderId = configuredValue;
        });

        // Assert
        Assert.Same(_mockBuilder.Object, result);
        _mockOptionsBuilder.Verify(x => x.Build(It.Is<GlobalOptions>(o => o.ProviderId == configuredValue)), Times.Once);
    }

    [Fact]
    public void WithTracking_GenericTContext_ShouldCreateTrackerEndpointFilterWithCorrectDependencies()
    {
        // Arrange
        var options = new GlobalOptions();
        var immutableOptions = new ImmutableGlobalOptions();
        EndpointFilterDelegate capturedNext = null;
        Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate> capturedFactory = null;

        _mockOptionsBuilder
            .Setup(x => x.Build<DbContext>(options))
            .Returns(immutableOptions);

        _mockBuilder
            .Setup(x => x.AddEndpointFilterFactory(It.IsAny<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()))
            .Callback<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>(factory => capturedFactory = factory)
            .Returns(_mockBuilder.Object);

        // Act
        _mockBuilder.Object.WithTracking<IEndpointConventionBuilder, DbContext>(options);

        // Create mock context and next delegate
        var mockFactoryContext = new EndpointFilterFactoryContext()
        {
            MethodInfo = null,
            ApplicationServices = _mockServiceProvider.Object
        };

        EndpointFilterDelegate next = (context) => ValueTask.FromResult<object?>(null);
        var filterDelegate = capturedFactory(mockFactoryContext, next);

        // Assert
        Assert.NotNull(capturedFactory);
        Assert.NotNull(filterDelegate);
        _mockOptionsBuilder.Verify(x => x.Build<DbContext>(options), Times.Once);
    }

    [Fact]
    public void WithTracking_NonGeneric_ShouldCreateTrackerEndpointFilterWithCorrectDependencies()
    {
        // Arrange
        var options = new GlobalOptions();
        var immutableOptions = new ImmutableGlobalOptions();
        EndpointFilterDelegate capturedNext = null;
        Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate> capturedFactory = null;

        _mockOptionsBuilder
            .Setup(x => x.Build(options))
            .Returns(immutableOptions);

        _mockBuilder
            .Setup(x => x.AddEndpointFilterFactory(It.IsAny<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()))
            .Callback<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>(factory => capturedFactory = factory)
            .Returns(_mockBuilder.Object);

        // Act
        _mockBuilder.Object.WithTracking(options);

        // Create mock context and next delegate
        var mockFactoryContext = new EndpointFilterFactoryContext()
        {
            MethodInfo = null,
            ApplicationServices = _mockServiceProvider.Object
        };

        EndpointFilterDelegate next = (context) => ValueTask.FromResult<object?>(null);
        var filterDelegate = capturedFactory(mockFactoryContext, next);

        // Assert
        Assert.NotNull(capturedFactory);
        Assert.NotNull(filterDelegate);
        _mockOptionsBuilder.Verify(x => x.Build(options), Times.Once);
    }

    [Fact]
    public void WithTracking_ShouldThrowWhenServicesAreNotRegistered()
    {
        // Arrange
        var emptyServices = new ServiceCollection();
        var emptyServiceProvider = emptyServices.BuildServiceProvider();
        var mockFactoryContext = new EndpointFilterFactoryContext()
        {
            MethodInfo = null,
            ApplicationServices = emptyServiceProvider
        };

        var options = new GlobalOptions();
        EndpointFilterDelegate capturedNext = null;
        Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate> capturedFactory = null;

        _mockBuilder
            .Setup(x => x.AddEndpointFilterFactory(It.IsAny<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()))
            .Callback<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>(factory => capturedFactory = factory)
            .Returns(_mockBuilder.Object);

        // Act
        _mockBuilder.Object.WithTracking(options);

        EndpointFilterDelegate next = (context) => ValueTask.FromResult<object?>(null);

        // Assert
        Assert.Throws<InvalidOperationException>(() => capturedFactory(mockFactoryContext, next));
    }

    [Fact]
    public void WithTracking_GenericTContext_ShouldUseCorrectBuildMethod()
    {
        // Arrange
        var options = new GlobalOptions();
        var immutableOptions = new ImmutableGlobalOptions();

        _mockOptionsBuilder
            .Setup(x => x.Build<SpecialDbContext>(options))
            .Returns(immutableOptions)
            .Verifiable();

        _mockBuilder
            .Setup(x => x.AddEndpointFilterFactory(It.IsAny<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()))
            .Returns(_mockBuilder.Object);

        // Act
        var result = _mockBuilder.Object.WithTracking<IEndpointConventionBuilder, SpecialDbContext>(options);

        // Assert
        Assert.Same(_mockBuilder.Object, result);
        _mockOptionsBuilder.Verify(x => x.Build<SpecialDbContext>(options), Times.Once);
    }

    [Fact]
    public void WithTracking_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var options = new GlobalOptions();
        var immutableOptions = new ImmutableGlobalOptions();

        _mockOptionsBuilder
            .Setup(x => x.Build<DbContext>(options))
            .Returns(immutableOptions);

        _mockBuilder
            .Setup(x => x.AddEndpointFilterFactory(It.IsAny<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()))
            .Returns(_mockBuilder.Object);

        // Act
        var result = _mockBuilder.Object.WithTracking<IEndpointConventionBuilder, DbContext>(options);

        // Assert
        Assert.Same(_mockBuilder.Object, result);
    }

    public class SpecialDbContext : DbContext { }
}
