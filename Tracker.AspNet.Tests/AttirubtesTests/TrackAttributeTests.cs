//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Abstractions;
//using Microsoft.AspNetCore.Mvc.Filters;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Moq;
//using NSubstitute;
//using System.Reflection;
//using Tracker.AspNet.Attributes;
//using Tracker.AspNet.Models;
//using Tracker.AspNet.Services.Contracts;
//using Tracker.Core.Services.Contracts;

//namespace Tracker.AspNet.Tests.AttirubtesTests;

//public class TrackAttributeTests
//{
//    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
//    private readonly Mock<IServiceScope> _serviceScopeMock;
//    private readonly Mock<IServiceProvider> _serviceProviderMock;

//    private readonly Mock<IProviderResolver> _providerResolverMock;
//    private readonly Mock<ISourceProvider> _sourceProvider;

//    private readonly Mock<IRequestFilter> _requestFilterMock;
//    private readonly Mock<IRequestHandler> _requestHandlerMock;
//    private readonly Mock<ILogger<TrackAttribute>> _loggerMock;

//    private readonly ActionExecutingContext _actionExecutingContext;
//    private readonly ImmutableGlobalOptions _defaultOptions;
//    private readonly HttpContext _httpContext;

//    public TrackAttributeTests()
//    {
//        _serviceProviderMock = new Mock<IServiceProvider>();
//        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
//        _serviceScopeMock = new Mock<IServiceScope>();

//        _providerResolverMock = new Mock<IProviderResolver>();
//        _sourceProvider = new Mock<ISourceProvider>();

//        _requestFilterMock = new Mock<IRequestFilter>();
//        _requestHandlerMock = new Mock<IRequestHandler>();
//        _loggerMock = new Mock<ILogger<TrackAttribute>>();

//        _httpContext = new DefaultHttpContext
//        {
//            RequestServices = _serviceProviderMock.Object
//        };

//        _defaultOptions = new ImmutableGlobalOptions
//        {
//            CacheControl = "max-age=3600",
//            Tables = []
//        };

//        var actionContext = new ActionContext(_httpContext, new(), new ActionDescriptor());
//        _actionExecutingContext = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), new object());
//    }

//    [Fact]
//    public void TrackAttribute_Constructor_SetsPropertiesCorrectly()
//    {
//        // Arrange
//        var tables = new[] { "table1", "table2" };
//        var sourceId = "custom-source";
//        var cacheControl = "no-cache";

//        // Act
//        var attribute = new TrackAttribute(tables, sourceId, cacheControl);

//        // Assert
//        Assert.NotNull(attribute);
//    }

//    [Fact]
//    public void TrackAttribute_AttributeUsage_IsCorrect()
//    {
//        // Arrange
//        var attributeUsage = typeof(TrackAttribute).GetCustomAttribute<AttributeUsageAttribute>();

//        // Assert
//        Assert.NotNull(attributeUsage);
//        Assert.Equal(AttributeTargets.Method, attributeUsage.ValidOn);
//        Assert.False(attributeUsage.AllowMultiple);
//        Assert.False(attributeUsage.Inherited);
//    }

//    [Fact]
//    public void GetOptions_FirstCall_BuildsOptionsCorrectly()
//    {
//        // Arrange
//        var attribute = new TrackAttribute(["users", "orders"], "custom-source", "no-store");

//        SetupScopeServiceProvider();
//        _sourceProvider.Setup(x => x.Id)
//            .Returns("custom-source");
//        _providerResolverMock.Setup(x => x.SelectProvider("custom-source", It.IsAny<ImmutableGlobalOptions>()))
//            .Returns(_sourceProvider.Object);

//        // Act
//        var result = attribute.GetOptions(_actionExecutingContext);

//        // Assert
//        Assert.Equal("no-store", result.CacheControl);
//        Assert.Contains("users", result.Tables);
//        Assert.Contains("orders", result.Tables);
//        Assert.Equal("custom-source", result.SourceProvider?.Id);
//        Assert.Equal(_sourceProvider.Object, result.SourceProvider);

//        Assert.Equal(_defaultOptions.Filter, result.Filter);
//        Assert.Equal(_defaultOptions.Suffix, result.Suffix);
//        Assert.Equal(_defaultOptions.SourceProviderFactory, result.SourceProviderFactory);
//        Assert.Equal(_defaultOptions.InvalidRequestDirectives, result.InvalidRequestDirectives);
//        Assert.Equal(_defaultOptions.InvalidResponseDirectives, result.InvalidResponseDirectives);
//    }

//    [Fact]
//    public void GetOptions_MutlipleCalls_ReturnsCachedOptions()
//    {
//        // Arrange
//        var attribute = new TrackAttribute(["users", "orders"], "custom-source", "no-store");

//        SetupScopeServiceProvider();
//        _sourceProvider.Setup(x => x.Id).Returns("custom-source");
//        _providerResolverMock.Setup(x => x.SelectProvider("custom-source", It.IsAny<ImmutableGlobalOptions>()))
//            .Returns(_sourceProvider.Object);

//        // Act
//        var result1 = attribute.GetOptions(_actionExecutingContext);
//        var result2 = attribute.GetOptions(_actionExecutingContext);
//        var result3 = attribute.GetOptions(_actionExecutingContext);

//        // Assert
//        Assert.Equal(result1, result2);
//        Assert.Equal(result2, result3);
//    }

//    [Fact]
//    public void GetOptions_WithPartialParameters_UsesDefaultsForMissingValues()
//    {
//        // Arrange
//        var attribute = new TrackAttribute(tables: ["products"]);

//        SetupScopeServiceProvider();
//        _serviceProviderMock.Setup(x => x.GetService(typeof(ImmutableGlobalOptions)))
//            .Returns(new ImmutableGlobalOptions
//            {
//                CacheControl = "max-age=7200",
//                SourceProvider = _sourceProvider.Object,
//                Tables = []
//            });
//        _providerResolverMock.Setup(x => x.SelectProvider(It.IsAny<string>(), It.IsAny<ImmutableGlobalOptions>()))
//           .Returns(_sourceProvider.Object);

//        // Act
//        var result = attribute.GetOptions(_actionExecutingContext);

//        // Assert
//        Assert.Contains("products", result.Tables);
//        Assert.Equal("max-age=7200", result.CacheControl);
//        Assert.Equal(_sourceProvider.Object, result.SourceProvider);
//    }

//    [Fact]
//    public void GetOptions_WithNullParameters_UsesDefaults()
//    {
//        // Arrange
//        var attribute = new TrackAttribute(null, null, null);

//        SetupScopeServiceProvider();
//        _providerResolverMock.Setup(x => x.SelectProvider(It.IsAny<string>(), It.IsAny<ImmutableGlobalOptions>()))
//          .Returns(_sourceProvider.Object);

//        // Act
//        var result = attribute.GetOptions(_actionExecutingContext);

//        // Assert
//        Assert.Equal(_defaultOptions.Tables, result.Tables);
//        Assert.Equal(_defaultOptions.CacheControl, result.CacheControl);
//        Assert.Equal(_sourceProvider.Object, result.SourceProvider);
//    }

//    [Fact]
//    public async Task GetOptions_ThreadSafety_MultipleThreadsShouldNotCreateMultipleInstances()
//    {
//        // Arrange
//        var count = 10;
//        var barrier = new Barrier(count);
//        var tasks = new List<Task>(count);
//        var attribute = new TrackAttribute();
//        var results = new List<ImmutableGlobalOptions>();

//        SetupScopeServiceProvider();

//        // Act
//        for (int i = 0; i < count; i++)
//        {
//            tasks.Add(Task.Run(() =>
//            {
//                barrier.SignalAndWait();
//                results.Add(attribute.GetOptions(_actionExecutingContext));
//            }));
//        }

//        await Task.WhenAll(tasks);

//        // Assert
//        var firstResult = results[0];
//        Assert.All(results, result => Assert.Same(firstResult, result));
//    }

//    [Fact]
//    public async Task OnActionExecutionAsync_RequestNotValid_ExecutesNextDelegate()
//    {
//        // Arrange
//        var attribute = new TrackAttribute();
//        var nextCalled = false;
//        var nextDelegate = new ActionExecutionDelegate(() =>
//        {
//            nextCalled = true;
//            return Task.FromResult<ActionExecutedContext>(null!);
//        });

//        SetupScopeServiceProvider();
//        _requestFilterMock.Setup(x => x.RequestValid(_httpContext, It.IsAny<ImmutableGlobalOptions>()))
//            .Returns(false);

//        // Act
//        await attribute.OnActionExecutionAsync(_actionExecutingContext, nextDelegate);

//        // Assert
//        Assert.True(nextCalled);
//        _requestHandlerMock.Verify(x => x.IsNotModified(It.IsAny<HttpContext>(), It.IsAny<ImmutableGlobalOptions>(), default),
//            Times.Never);
//    }

//    [Fact]
//    public async Task OnActionExecutionAsync_RequestValidButModified_ExecutesNextDelegate()
//    {
//        // Arrange
//        var attribute = new TrackAttribute();
//        var nextCalled = false;
//        var nextDelegate = new ActionExecutionDelegate(() =>
//        {
//            nextCalled = true;
//            return Task.FromResult<ActionExecutedContext>(null!);
//        });

//        SetupScopeServiceProvider();
//        _requestFilterMock.Setup(x => x.RequestValid(_httpContext, It.IsAny<ImmutableGlobalOptions>()))
//            .Returns(true);
//        _requestHandlerMock.Setup(x => x.IsNotModified(_httpContext, It.IsAny<ImmutableGlobalOptions>(), default))
//            .ReturnsAsync(false);

//        // Act
//        await attribute.OnActionExecutionAsync(_actionExecutingContext, nextDelegate);

//        // Assert
//        Assert.True(nextCalled);
//        _requestHandlerMock.Verify(x => x.IsNotModified(_httpContext, It.IsAny<ImmutableGlobalOptions>(), default), Times.Once);
//    }

//    [Fact]
//    public async Task OnActionExecutionAsync_RequestValidAndNotModified_DoesNotExecuteNextDelegate()
//    {
//        // Arrange
//        var attribute = new TrackAttribute();
//        var nextCalled = false;
//        var nextDelegate = new ActionExecutionDelegate(() =>
//        {
//            nextCalled = true;
//            return Task.FromResult<ActionExecutedContext>(null!);
//        });

//        SetupScopeServiceProvider();
//        _requestFilterMock.Setup(x => x.RequestValid(_httpContext, It.IsAny<ImmutableGlobalOptions>()))
//            .Returns(true);
//        _requestHandlerMock.Setup(x => x.IsNotModified(_httpContext, It.IsAny<ImmutableGlobalOptions>(), default))
//            .ReturnsAsync(true);

//        // Act
//        await attribute.OnActionExecutionAsync(_actionExecutingContext, nextDelegate);

//        // Assert
//        Assert.False(nextCalled);
//        _requestHandlerMock.Verify(x => x.IsNotModified(_httpContext, It.IsAny<ImmutableGlobalOptions>(), default), Times.Once);
//    }

//    [Fact]
//    public async Task OnActionExecutionAsync_ServiceResolutionFails_ThrowsException()
//    {
//        // Arrange
//        var attribute = new TrackAttribute();
//        var nextDelegate = new ActionExecutionDelegate(() =>
//            Task.FromResult<ActionExecutedContext>(null!));

//        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestFilter)))
//            .Returns(null); // Simulate missing service

//        // Act & Assert
//        await Assert.ThrowsAsync<InvalidOperationException>(() =>
//            attribute.OnActionExecutionAsync(_actionExecutingContext, nextDelegate));
//    }

//    private void SetupScopeServiceProvider()
//    {
//        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
//            .Returns(_serviceScopeFactoryMock.Object);

//        _serviceScopeFactoryMock.Setup(x => x.CreateScope())
//            .Returns(_serviceScopeMock.Object);

//        _serviceScopeMock.Setup(x => x.ServiceProvider)
//            .Returns(_serviceProviderMock.Object);

//        _serviceScopeMock.Setup(x => x.Dispose())
//            .Verifiable();

//        SetupServiceProvider();
//    }

//    private void SetupServiceProvider()
//    {
//        _serviceProviderMock.Setup(x => x.GetService(typeof(IProviderResolver)))
//            .Returns(_providerResolverMock.Object);

//        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestFilter)))
//            .Returns(_requestFilterMock.Object);

//        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestFilter)))
//            .Returns(_requestFilterMock.Object);

//        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestHandler)))
//            .Returns(_requestHandlerMock.Object);

//        _serviceProviderMock.Setup(x => x.GetService(typeof(ImmutableGlobalOptions)))
//            .Returns(_defaultOptions);

//        _serviceProviderMock.Setup(x => x.GetService(typeof(ILogger<TrackAttribute>)))
//            .Returns(_loggerMock.Object);
//    }
//}