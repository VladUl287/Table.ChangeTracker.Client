using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Immutable;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Tests;

public class RequestHandlerTests
{
    private readonly Mock<IETagProvider> _mockETagService;
    private readonly Mock<ISourceOperationsResolver> _mockOperationsResolver;
    private readonly Mock<ITimestampsHasher> _mockTimestampsHasher;
    private readonly Mock<ILogger<DefaultRequestHandler>> _mockLogger;
    private readonly DefaultRequestHandler _handler;

    public RequestHandlerTests()
    {
        _mockETagService = new Mock<IETagProvider>();
        _mockOperationsResolver = new Mock<ISourceOperationsResolver>();
        _mockTimestampsHasher = new Mock<ITimestampsHasher>();
        _mockLogger = new Mock<ILogger<DefaultRequestHandler>>();

        _handler = new DefaultRequestHandler(
            _mockETagService.Object,
            _mockOperationsResolver.Object,
            _mockTimestampsHasher.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDependenciesAreNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultRequestHandler(null, _mockOperationsResolver.Object, _mockTimestampsHasher.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultRequestHandler(_mockETagService.Object, null, _mockTimestampsHasher.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultRequestHandler(_mockETagService.Object, _mockOperationsResolver.Object, null, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultRequestHandler(_mockETagService.Object, _mockOperationsResolver.Object, _mockTimestampsHasher.Object, null));
    }

    [Fact]
    public async Task IsNotModified_ShouldThrowArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var options = new ImmutableGlobalOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _handler.IsNotModified(null, options, CancellationToken.None));
    }

    [Fact]
    public async Task IsNotModified_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _handler.IsNotModified(context, null, CancellationToken.None));
    }

    [Fact]
    public async Task IsNotModified_ShouldReturnNotModified_WhenETagMatches()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.IfNoneMatch = "\"test-etag\"";
        context.TraceIdentifier = "test-trace";

        var options = new ImmutableGlobalOptions
        {
            Tables = [],
            CacheControl = "no-cache"
        };

        var mockSourceOperations = new Mock<ISourceOperations>();
        mockSourceOperations.Setup(x => x.GetLastTimestamp(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        mockSourceOperations.Setup(x => x.SourceId).Returns("test-source");

        //_mockOperationsResolver.Setup(x => x.TryResolve(It.IsAny<string>()))
        //    .Returns(mockSourceOperations.Object);

        _mockETagService.Setup(x => x.Compare("\"test-etag\"", It.IsAny<ulong>(), It.IsAny<string>()))
            .Returns(true);
        _mockETagService.Setup(x => x.Generate(It.IsAny<ulong>(), It.IsAny<string>()))
            .Returns("\"new-etag\"");

        // Act
        var result = await _handler.IsNotModified(context, options, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status304NotModified, context.Response.StatusCode);
    }

    [Fact]
    public async Task IsNotModified_ShouldReturnModified_WhenETagDoesNotMatch()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.IfNoneMatch = "\"old-etag\"";
        context.TraceIdentifier = "test-trace";

        var options = new ImmutableGlobalOptions
        {
            Tables = [],
            CacheControl = "max-age=3600"
        };

        var mockSourceOperations = new Mock<ISourceOperations>();
        mockSourceOperations.Setup(x => x.GetLastTimestamp(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        mockSourceOperations.Setup(x => x.SourceId).Returns("test-source");

        //_mockOperationsResolver.Setup(x => x.TryResolve(It.IsAny<string>()))
        //    .Returns(mockSourceOperations.Object);

        _mockETagService.Setup(x => x.Compare("\"old-etag\"", It.IsAny<ulong>(), It.IsAny<string>()))
            .Returns(false);
        _mockETagService.Setup(x => x.Generate(It.IsAny<ulong>(), It.IsAny<string>()))
            .Returns("\"new-etag\"");

        // Act
        var result = await _handler.IsNotModified(context, options, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Equal("\"new-etag\"", context.Response.Headers.ETag);
        Assert.Equal("max-age=3600", context.Response.Headers.CacheControl);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task GetLastTimestampValue_ShouldHandleDifferentTableCounts(int tableCount)
    {
        // Arrange
        var options = new ImmutableGlobalOptions
        {
            Tables = [.. Enumerable.Range(0, tableCount).Select(i => $"Table{i}")]
        };

        var mockSourceOperations = new Mock<ISourceOperations>();
        var now = DateTimeOffset.UtcNow;

        if (tableCount == 0)
        {
            mockSourceOperations.Setup(x => x.GetLastTimestamp(It.IsAny<CancellationToken>()))
                .ReturnsAsync(now);
        }
        else if (tableCount == 1)
        {
            mockSourceOperations.Setup(x => x.GetLastTimestamp(options.Tables[0], It.IsAny<CancellationToken>()))
                .ReturnsAsync(now);
        }
        else
        {
            var timestamps = new DateTimeOffset[tableCount];
            Array.Fill(timestamps, now);

            mockSourceOperations.Setup(x => x.GetLastTimestamps(
                    options.Tables,
                    It.IsAny<DateTimeOffset[]>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ImmutableArray<string>, DateTimeOffset[], CancellationToken>((tables, output, token) =>
                {
                    for (int i = 0; i < tables.Length; i++)
                    {
                        output[i] = now;
                    }
                })
                .Returns(ValueTask.CompletedTask);

            //_mockTimestampsHasher.Setup(x => x.Hash(It.IsAny<ReadOnlySpan<DateTimeOffset>>()))
            //    .Returns(123456UL);
        }

        // Act
        var method = typeof(DefaultRequestHandler)
            .GetMethod("GetLastTimestampValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = await (Task<ulong>)method.Invoke(_handler, new object[] { options, mockSourceOperations.Object, CancellationToken.None });

        // Assert
        if (tableCount >= 2)
        {
            Assert.Equal(123456UL, result);
        }
        else
        {
            Assert.Equal((ulong)now.Ticks, result);
        }
    }
}