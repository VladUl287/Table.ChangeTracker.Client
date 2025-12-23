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
public class TrackerMiddlewareFlterBenchmark
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
        var timestampHasher = new DefaultTrackerHasher();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Error);
        });

        var loggerHandler = loggerFactory.CreateLogger<DefaultRequestHandler>();
        var defaultRequestHandler = new DefaultRequestHandler(
            etagProvider, timestampHasher, loggerHandler);

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

    private sealed class BenchmarkOperationsProvider : ISourceProvider
    {
        public string Id => "1";

        public ValueTask<bool> DisableTracking(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> EnableTracking(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<long> GetLastVersion(string key, CancellationToken token = default)
        {
            return new ValueTask<long>(638081280000000000);
        }

        public ValueTask<long> GetLastVersion(CancellationToken token = default)
        {
            return new ValueTask<long>(638081280000000000);
        }

        public ValueTask GetLastVersions(ImmutableArray<string> keys, long[] timestamps, CancellationToken token = default)
        {
            timestamps[0] = 638081280000000000;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> IsTracking(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> SetLastVersion(string key, long value, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
