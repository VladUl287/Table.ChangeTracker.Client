using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

/// <summary>
/// Basic implementation of <see cref="IRequestHandler"/> which determines if the requested data has not been modified, 
/// allowing a 304 Not Modified status code to be returned.
/// </summary>
public sealed class DefaultRequestHandler(
    IETagProvider eTagService, ISourceOperationsResolver operationsResolver, ITrackerHasher timestampsHasher,
    ILogger<DefaultRequestHandler> logger) : IRequestHandler
{
    public async ValueTask<bool> IsNotModified(HttpContext ctx, ImmutableGlobalOptions options, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var traceId = new TraceId(ctx);

        logger.LogRequestHandleStarted(traceId, ctx.Request.Path);
        try
        {
            var operationProvider = GetOperationsProvider(ctx, options);
            logger.LogSourceProviderResolved(traceId, operationProvider.SourceId);

            var lastTimestamp = await GetLastVersionAsync(options, operationProvider);

            var ifNoneMatch = ctx.Request.Headers.IfNoneMatch.Count > 0 ? ctx.Request.Headers.IfNoneMatch[0] : null;

            var suffix = options.Suffix(ctx);
            if (ifNoneMatch is not null && eTagService.Compare(ifNoneMatch, lastTimestamp, suffix))
            {
                ctx.Response.StatusCode = StatusCodes.Status304NotModified;
                logger.LogNotModified(traceId, ifNoneMatch);
                return true;
            }

            var etag = eTagService.Generate(lastTimestamp, suffix);
            ctx.Response.Headers.CacheControl = options.CacheControl;
            ctx.Response.Headers.ETag = etag;
            logger.LogETagAdded(etag, traceId);
            return false;
        }
        finally
        {
            logger.LogRequestHandleFinished(traceId);
        }
    }

    private async ValueTask<ulong> GetLastVersionAsync(ImmutableGlobalOptions options, ISourceOperations sourceOperations)
    {
        switch (options.Tables.Length)
        {
            case 0:
                var timestamp = await sourceOperations.GetLastVersion(default);
                return (ulong)timestamp;
            case 1:
                var tableName = options.Tables[0];
                var singleTableTimestamp = await sourceOperations.GetLastVersion(tableName, default);
                return (ulong)singleTableTimestamp;
            default:
                var timestamps = ArrayPool<long>.Shared.Rent(options.Tables.Length);
                await sourceOperations.GetLastVersions(options.Tables, timestamps, default);
                var hash = timestampsHasher.Hash(timestamps.AsSpan(0, options.Tables.Length));
                ArrayPool<long>.Shared.Return(timestamps);
                return hash;
        }
    }

    private ISourceOperations GetOperationsProvider(HttpContext ctx, ImmutableGlobalOptions opt)
    {
        var traceId = new TraceId(ctx);

        if (opt.Source is not null)
        {
            if (operationsResolver.TryResolve(opt.Source, out var provider))
                return provider;

            logger.LogSourceProviderNotRegistered(opt.Source, traceId);
        }

        return
            opt.SourceOperations ??
            opt.SourceOperationsFactory?.Invoke(ctx) ??
            operationsResolver.First ??
            throw new NullReferenceException($"Source operations provider not found. TraceId - '{ctx.TraceIdentifier}'");
    }
}
