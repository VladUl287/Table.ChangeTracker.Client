using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;
using Tracker.Core.Utils;

namespace Tracker.AspNet.Services;

public class RequestHandler(
    IETagService eTagService, ISourceOperationsResolver operationsResolver, ITimestampsHasher timestampsHasher,
    ILogger<RequestHandler> logger) : IRequestHandler
{
    public async Task<bool> IsNotModified(HttpContext ctx, ImmutableGlobalOptions options, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var sourceOperations = GetOperationsProvider(ctx, options, operationsResolver);

        var ltValue = await GetLastTimestampValue(options, sourceOperations, token);

        var srcETag = ctx.Request.Headers.IfNoneMatch.Count > 0 ? ctx.Request.Headers.IfNoneMatch[0] : null;

        var asBuildTime = eTagService.AssemblyBuildTimeTicks;
        var ltDigitCount = UlongUtils.DigitCount(ltValue);
        var suffix = options.Suffix(ctx);
        var fullLength = asBuildTime.Length + 1 + ltDigitCount + suffix.Length + (suffix.Length > 0 ? 1 : 0);

        if (srcETag is not null && eTagService.EqualsTo(fullLength, srcETag, ltValue, suffix))
        {
            ctx.Response.StatusCode = StatusCodes.Status304NotModified;
            logger.LogNotModified(srcETag);
            return true;
        }

        ctx.Response.Headers.CacheControl = options.CacheControl;
        var etag = eTagService.Build(fullLength, ltValue, suffix);
        ctx.Response.Headers.ETag = etag;
        logger.LogETagAdded(etag);
        return false;
    }

    private async Task<ulong> GetLastTimestampValue(ImmutableGlobalOptions options, ISourceOperations sourceOperations, CancellationToken token)
    {
        if (options is { Tables.Length: 0 })
        {
            var tm = await sourceOperations.GetLastTimestamp(token);
            return (ulong)tm.Ticks;
        }

        if (options is { Tables.Length: 1 })
        {
            var tm = await sourceOperations.GetLastTimestamp(options.Tables[0], token);
            return (ulong)tm.Ticks;
        }

        var timestamps = ArrayPool<DateTimeOffset>.Shared.Rent(options.Tables.Length);
        await sourceOperations.GetLastTimestamps(options.Tables, timestamps, token);
        var result = timestampsHasher.Hash(timestamps.AsSpan(0, options.Tables.Length));
        ArrayPool<DateTimeOffset>.Shared.Return(timestamps);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ISourceOperations GetOperationsProvider(
        HttpContext ctx, ImmutableGlobalOptions opt, ISourceOperationsResolver resolver) =>
        opt.SourceOperations ?? opt.SourceOperationsFactory?.Invoke(ctx) ?? resolver.Resolve(opt.Source);
}
