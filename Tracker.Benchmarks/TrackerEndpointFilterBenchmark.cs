using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using Tracker.AspNet.Middlewares;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.Core.Services;
using Tracker.Core.Services.Contracts;

namespace Tracker.Benchmarks;

[MemoryDiagnoser]
public class TrackerEndpointFilterBenchmark
{
    private static readonly ImmutableGlobalOptions emptyGlobalOptions = new()
    {
        Tables = ["roles"]
    };
    private TrackerMiddleware trackerMiddleware;
    private HttpContext httpContext;
    private HttpContext httpContextNotModified;
    private HttpContext httpContextPostMethod;
    private HttpContext httpContextResponseImmutable;

    [IterationSetup]
    public void Setup()
    {
        var etagProvider = new DefaultETagProvider(new BenchAssemblyProvider());
        var sourceOpeationsResolver = new SourceOperationsResolver([new BenchmarkOperationsProvider()]);
        var timestampHasher = new XxHash64Hasher();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Error);
        });

        var loggerHandler = loggerFactory.CreateLogger<DefaultRequestHandler>();
        var defaultRequestHandler = new DefaultRequestHandler(
            etagProvider, sourceOpeationsResolver, timestampHasher, loggerHandler);

        var logger = loggerFactory.CreateLogger<DefaultRequestFilter>();
        var requestFilter = new DefaultRequestFilter(new DefaltDirectiveChecker(), logger);

        loggerFactory.Dispose();

        static Task next(HttpContext ctx) => Task.CompletedTask;

        trackerMiddleware = new(next, requestFilter, defaultRequestHandler, emptyGlobalOptions);

        httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;

        httpContextPostMethod = new DefaultHttpContext();
        httpContextPostMethod.Request.Method = HttpMethods.Post;

        httpContextNotModified = new DefaultHttpContext();
        httpContextNotModified.Request.Method = HttpMethods.Get;
        httpContextNotModified.Request.Headers.IfNoneMatch = "638081280000000000-638081280000000000";

        httpContextResponseImmutable = new DefaultHttpContext();
        httpContextResponseImmutable.Request.Method = HttpMethods.Get;
        httpContextResponseImmutable.Response.Headers.CacheControl = "immutable";
    }

    [Benchmark]
    public Task Middleware_GenerateEtag() => trackerMiddleware.InvokeAsync(httpContext);

    [Benchmark]
    public Task Middleware_NotModified() => trackerMiddleware.InvokeAsync(httpContextNotModified);

    [Benchmark]
    public Task Middleware_PostMethod() => trackerMiddleware.InvokeAsync(httpContextPostMethod);

    [Benchmark]
    public Task Middleware_ResponseImmutable() => trackerMiddleware.InvokeAsync(httpContextResponseImmutable);

    private sealed class BenchAssemblyProvider : IAssemblyTimestampProvider
    {
        private static readonly DateTimeOffset _timestamp = DateTimeOffset.Parse("2023-01-01");

        public DateTimeOffset GetWriteTime() => _timestamp;
    }

    private sealed class BenchmarkOperationsProvider : ISourceOperations
    {
        public string SourceId => string.Empty;

        public Task<bool> DisableTracking(string key, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<bool> EnableTracking(string key, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        private static readonly DateTimeOffset _timestamp = DateTimeOffset.Parse("2023-01-01");

        public Task<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token)
        {
            return Task.FromResult(_timestamp);
        }

        public Task<DateTimeOffset> GetLastTimestamp(CancellationToken token)
        {
            return Task.FromResult(_timestamp);
        }

        public Task GetLastTimestamps(ImmutableArray<string> keys, DateTimeOffset[] timestamps, CancellationToken token)
        {
            timestamps[0] = _timestamp;
            return Task.CompletedTask;
        }

        public Task<bool> IsTracking(string key, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetLastTimestamp(string key, DateTimeOffset value, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
