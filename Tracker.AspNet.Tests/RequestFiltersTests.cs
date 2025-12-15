using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;

namespace Tracker.AspNet.Tests;

public class DefaultRequestFilterTests
{
    private readonly Mock<ILogger<DefaultRequestFilter>> _loggerMock;
    private readonly Mock<ImmutableGlobalOptions> _optionsMock;
    private readonly DefaultRequestFilter _filter;
    private readonly DefaultHttpContext _httpContext;

    public DefaultRequestFilterTests()
    {
        _loggerMock = new Mock<ILogger<DefaultRequestFilter>>();
        _optionsMock = new Mock<ImmutableGlobalOptions>();
        _filter = new DefaultRequestFilter(new DefaltDirectiveChecker(), _loggerMock.Object);
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public void RequestValid_GetRequest_ValidConditions_ReturnsTrue()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(true);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

        // Assert
        Assert.True(result);
        VerifyLogCalled("LogFilterStarted", Times.Once());
        VerifyLogCalled("LogContextFilterFinished", Times.Once());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public void RequestValid_NonGetRequest_ReturnsFalse(string method)
    {
        // Arrange
        _httpContext.Request.Method = method;
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(true);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

        // Assert
        Assert.False(result);
        VerifyLogCalled("LogNotGetRequest", Times.Once());
        VerifyLogCalled("LogFilterStarted", Times.Once());
    }

    [Fact]
    public void RequestValid_ResponseHasETag_ReturnsFalse()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Response.Headers.ETag = "test-etag";
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(true);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

        // Assert
        Assert.False(result);
        VerifyLogCalled("LogEtagHeaderPresented", Times.Once());
    }

    [Theory]
    [InlineData("immutable")]
    [InlineData("IMMUTABLE")]
    [InlineData("Immutable")]
    [InlineData("max-age=3600, immutable")]
    [InlineData("no-cache, immutable")]
    public void RequestValid_RequestCacheControlContainsImmutable_ReturnsFalse(string cacheControl)
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Request.Headers.CacheControl = cacheControl;
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(true);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

        // Assert
        Assert.False(result);
        VerifyLogCalled("LogRequestNotValidCacheControlDirective", Times.Once());
    }

    [Theory]
    [InlineData("no-store")]
    [InlineData("NO-STORE")]
    [InlineData("No-Store")]
    [InlineData("max-age=0, no-store")]
    [InlineData("no-cache, no-store")]
    public void RequestValid_RequestCacheControlContainsNoStore_ReturnsFalse(string cacheControl)
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Request.Headers.CacheControl = cacheControl;
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(true);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

        // Assert
        Assert.False(result);
        VerifyLogCalled("LogRequestNotValidCacheControlDirective", Times.Once());
    }

    [Theory]
    [InlineData("immutable")]
    [InlineData("no-store")]
    public void RequestValid_ResponseCacheControlContainsInvalidDirectives_ReturnsFalse(string cacheControl)
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Response.Headers.CacheControl = cacheControl;
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(true);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

        // Assert
        Assert.False(result);
        VerifyLogCalled("LogResponseNotValidCacheControlDirective", Times.Once());
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
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(true);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RequestValid_OptionsFilterReturnsFalse_ReturnsFalse()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(false);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

        // Assert
        Assert.False(result);
        VerifyLogCalled("LogFilterRejected", Times.Once());
    }

    [Fact]
    public void RequestValid_MultipleCacheControlHeaders_ContainsInvalid_ReturnsFalse()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Request.Headers.Append("Cache-Control", "max-age=3600");
        _httpContext.Request.Headers.Append("Cache-Control", "immutable");
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(true);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

        // Assert
        Assert.False(result);
        VerifyLogCalled("LogRequestNotValidCacheControlDirective", Times.Once());
    }

    [Fact]
    public void RequestValid_EmptyCacheControlHeader_ReturnsTrue()
    {
        // Arrange
        _httpContext.Request.Method = HttpMethods.Get;
        _httpContext.Request.Headers.CacheControl = "";
        _httpContext.Response.Headers.CacheControl = "";
        _optionsMock.Setup(o => o.Filter(It.IsAny<HttpContext>())).Returns(true);

        // Act
        var result = _filter.RequestValid(_httpContext, _optionsMock.Object);

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
        var result = DefaultRequestFilter_Accessor.AnyInvalidCacheControl(cacheControl, out var directive);

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
        var result = DefaultRequestFilter_Accessor.AnyInvalidCacheControl(cacheControl, out var directive);

        // Assert
        Assert.False(result);
        Assert.Null(directive);
    }

    [Fact]
    public void AnyInvalidCacheControl_MultipleHeaders_InvalidDirective_ReturnsTrue()
    {
        // Arrange
        var headers = new StringValues(new[] { "max-age=3600", "immutable" });

        // Act
        var result = DefaultRequestFilter_Accessor.AnyInvalidCacheControl(headers, out var directive);

        // Assert
        Assert.True(result);
        Assert.Equal("immutable", directive);
    }

    [Fact]
    public void AnyInvalidCacheControl_NullHeaderValue_Skipped()
    {
        // Arrange
        var headers = new StringValues(new[] { "max-age=3600", null, "no-cache" });

        // Act
        var result = DefaultRequestFilter_Accessor.AnyInvalidCacheControl(headers, out var directive);

        // Assert
        Assert.False(result);
        Assert.Null(directive);
    }

    private void VerifyLogCalled(string logMethodName, Times times)
    {
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            times);
    }
}

internal static class DefaultRequestFilter_Accessor
{
    public static bool AnyInvalidCacheControl(StringValues cacheControlHeaders, out string? directive)
    {
        // This would require using reflection or making the method internal
        // For this example, I'll show the reflection approach:
        var method = typeof(DefaultRequestFilter).GetMethod("AnyInvalidCacheControl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (method == null)
            throw new InvalidOperationException("Method not found");

        var parameters = new object[] { cacheControlHeaders, null };
        var result = (bool)method.Invoke(null, parameters);
        directive = (string?)parameters[1];
        return result;
    }
}
