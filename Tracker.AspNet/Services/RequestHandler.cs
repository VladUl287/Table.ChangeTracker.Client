using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class RequestHandler(
    IETagService eTagService, ISourceOperationsResolver operationsResolver, ITimestampsHasher timestampsHasher,
    ILogger<RequestHandler> logger) : IRequestHandler
{
    public async Task<bool> IsNotModified(HttpContext ctx, ImmutableGlobalOptions options, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        logger.LogRequestHandleStarted(ctx.TraceIdentifier);
        try
        {
            var operationProvider = GetOperationsProvider(ctx, options, operationsResolver);
            var lastTimestamp = await GetLastTimestampValue(options, operationProvider, token);

            var ifNoneMatch = ctx.Request.Headers.IfNoneMatch.Count > 0 ? ctx.Request.Headers.IfNoneMatch[0] : null;

            var suffix = options.Suffix(ctx);
            if (ifNoneMatch is not null && eTagService.EqualsTo(ifNoneMatch, lastTimestamp, suffix))
            {
                ctx.Response.StatusCode = StatusCodes.Status304NotModified;
                logger.LogNotModified(ctx.TraceIdentifier, ifNoneMatch);
                return true;
            }

            var etag = eTagService.Build(lastTimestamp, suffix);
            ctx.Response.Headers.CacheControl = options.CacheControl;
            ctx.Response.Headers.ETag = etag;
            logger.LogETagAdded(etag, ctx.TraceIdentifier);
            return false;
        }
        finally
        {
            logger.LogRequestHandleFinished(ctx.TraceIdentifier);
        }
    }

    private async Task<ulong> GetLastTimestampValue(ImmutableGlobalOptions options, ISourceOperations sourceOperations, CancellationToken token)
    {
        if (options is { Tables.Length: 0 })
            return (ulong)(await sourceOperations.GetLastTimestamp(token)).Ticks;

        if (options is { Tables.Length: 1 })
            return (ulong)(await sourceOperations.GetLastTimestamp(options.Tables[0], token)).Ticks;

        var timestamps = ArrayPool<DateTimeOffset>.Shared.Rent(options.Tables.Length);
        await sourceOperations.GetLastTimestamps(options.Tables, timestamps, token);
        var hash = timestampsHasher.Hash(timestamps.AsSpan(0, options.Tables.Length));
        ArrayPool<DateTimeOffset>.Shared.Return(timestamps);
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ISourceOperations GetOperationsProvider(
        HttpContext ctx, ImmutableGlobalOptions opt, ISourceOperationsResolver resolver) =>
        opt.SourceOperations ?? opt.SourceOperationsFactory?.Invoke(ctx) ?? resolver.Resolve(opt.Source) ??
        throw new NullReferenceException($"Fail to resolve source operations provider for request '{ctx.Request.Path}'");
}
