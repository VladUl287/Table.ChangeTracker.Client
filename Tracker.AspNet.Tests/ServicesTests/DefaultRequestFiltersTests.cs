using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;

namespace Tracker.AspNet.Tests.ServicesTests;

public class DefaultRequestFilterTests
{
    private readonly Mock<ILogger<DefaultRequestFilter>> _loggerMock;
    private readonly DefaultRequestFilter _filter;
    private readonly DefaultHttpContext _httpContext;

    public DefaultRequestFilterTests()
    {
        _loggerMock = new Mock<ILogger<DefaultRequestFilter>>();
        _filter = new DefaultRequestFilter(_loggerMock.Object);
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public void RequestValid_GetRequest_ValidConditions_ReturnsTrue()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        var options = new ImmutableGlobalOptions();

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("CONNECT")]
    [InlineData("TRACE")]
    public void RequestValid_NonGetRequest_ReturnsFalse(string method)
    {
        // Arrange
        _httpContext.Request.Method = method;
        var options = new ImmutableGlobalOptions();

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RequestValid_ResponseHasETag_ReturnsFalse()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Response.Headers.ETag = "test-etag";
        var options = new ImmutableGlobalOptions();

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("no-store")]
    [InlineData("NO-STORE")]
    [InlineData("No-Store")]
    [InlineData("max-age=0, no-store")]
    [InlineData("no-cache, no-store")]
    public void RequestValid_RequestCacheControlContainsInvalidDirectives_ReturnsFalse(string cacheControl)
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Request.Headers.CacheControl = cacheControl;
        var options = new ImmutableGlobalOptions()
        {
            InvalidRequestDirectives = ["no-store"]
        };

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("immutable")]
    [InlineData("no-store")]
    [InlineData("IMMUTABLE")]
    [InlineData("Immutable")]
    [InlineData("max-age=3600, immutable")]
    [InlineData("no-cache, immutable")]
    public void RequestValid_ResponseCacheControlContainsInvalidDirectives_ReturnsFalse(string cacheControl)
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Response.Headers.CacheControl = cacheControl;
        var options = new ImmutableGlobalOptions()
        {
            InvalidResponseDirectives = ["no-store", "immutable"]
        };

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("max-age=3600")]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("no-cache")]
    [InlineData("must-revalidate")]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValid_ValidCacheControl_ReturnsTrue(string cacheControl)
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        if (cacheControl != null)
        {
            _httpContext.Request.Headers.CacheControl = cacheControl;
            _httpContext.Response.Headers.CacheControl = cacheControl;
        }
        var options = new ImmutableGlobalOptions()
        {
            InvalidRequestDirectives = ["no-store"],
            InvalidResponseDirectives = ["no-store", "immutable"]
        };

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RequestValid_MultipleCacheControlHeaders_ContainsInvalid_ReturnsFalse()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Response.Headers.Append("Cache-Control", "max-age=3600");
        _httpContext.Response.Headers.Append("Cache-Control", "immutable");
        var options = new ImmutableGlobalOptions()
        {
            InvalidResponseDirectives = ["no-store", "immutable"]
        };

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RequestValid_MultipleCacheControlHeaders_EmptyOptionsList_ReturnsTrue()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Response.Headers.Append("Cache-Control", "max-age=3600");
        _httpContext.Response.Headers.Append("Cache-Control", "immutable");
        var options = new ImmutableGlobalOptions();

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RequestValid_EmptyCacheControlHeader_ReturnsTrue()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Request.Headers.CacheControl = "";
        _httpContext.Response.Headers.CacheControl = "";
        var options = new ImmutableGlobalOptions()
        {
            InvalidResponseDirectives = ["no-store", "immutable"]
        };

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("immutable", "immutable")]
    [InlineData("no-store", "no-store")]
    [InlineData("IMMUTABLE", "immutable")]
    [InlineData("NO-STORE", "no-store")]
    [InlineData("max-age=3600, immutable", "immutable")]
    [InlineData("no-store, max-age=0", "no-store")]
    public void AnyInvalidCacheControl_InvalidDirective_ReturnsTrueWithCorrectDirective(string cacheControl, string expectedDirective)
    {
        // Act
        var result = DefaultRequestFilter.AnyInvalidDirective(cacheControl, ["no-store", "immutable"], out var directive);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedDirective, directive);
    }

    [Theory]
    [InlineData("max-age=3600")]
    [InlineData("public, max-age=31536000")]
    [InlineData("no-cache")]
    [InlineData("")]
    [InlineData(null)]
    public void AnyInvalidCacheControl_ValidDirective_ReturnsFalse(string cacheControl)
    {
        // Act
        var result = DefaultRequestFilter.AnyInvalidDirective(cacheControl, ["no-store", "immutable"], out var directive);

        // Assert
        Assert.False(result);
        Assert.Null(directive);
    }

    [Fact]
    public void AnyInvalidCacheControl_MultipleHeaders_InvalidDirective_ReturnsTrue()
    {
        // Arrange
        var headers = new StringValues(["max-age=3600", "immutable"]);

        // Act
        var result = DefaultRequestFilter.AnyInvalidDirective(headers, ["no-store", "immutable"], out var directive);

        // Assert
        Assert.True(result);
        Assert.Equal("immutable", directive);
    }

    [Fact]
    public void AnyInvalidCacheControl_NullHeaderValue_Skipped()
    {
        // Arrange
        var headers = new StringValues(["max-age=3600", null, "no-cache"]);

        // Act
        var result = DefaultRequestFilter.AnyInvalidDirective(headers, ["no-store", "immutable"], out var directive);

        // Assert
        Assert.False(result);
        Assert.Null(directive);
    }


    [Fact]
    public void RequestValid_OptionsFilterReturnsFalse_ReturnsFalse()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        var options = new ImmutableGlobalOptions()
        {
            Filter = (_) => false
        };

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RequestValid_OptionsFilterReturnsTrue_ReturnsTrue()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        var options = new ImmutableGlobalOptions()
        {
            Filter = (_) => true
        };

        // Act
        var result = _filter.ValidRequest(_httpContext, options);

        // Assert
        Assert.True(result);
    }
}
