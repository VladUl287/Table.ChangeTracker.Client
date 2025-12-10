using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;
using Tracker.Core.Services.Contracts;
using Tracker.Core.Utils;

namespace Tracker.AspNet.Services;

public class RequestHandler(
    IETagGenerator etagGenerator, ISourceOperationsResolver operationsResolver, ITimestampsHasher timestampsHasher,
    ILogger<RequestHandler> logger) : IRequestHandler
{
    public async Task<bool> IsNotModified(HttpContext ctx, ImmutableGlobalOptions options, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var sourceOperations = GetOperationsProvider(ctx, options, operationsResolver);

        ulong ltValue;
        if (options is { Tables.Length: 0 })
        {
            var tm = await sourceOperations.GetLastTimestamp(token);
            ltValue = (ulong)tm.Ticks;
        }
        else if (options is { Tables.Length: 1 })
        {
            var tm = await sourceOperations.GetLastTimestamp(options.Tables[0], token);
            ltValue = (ulong)tm.Ticks;
        }
        else
        {
            var timestamps = ArrayPool<DateTimeOffset>.Shared.Rent(options.Tables.Length);
            await sourceOperations.GetLastTimestamps(options.Tables, timestamps, token);
            ltValue = timestampsHasher.Hash(timestamps.AsSpan(0, options.Tables.Length));
            ArrayPool<DateTimeOffset>.Shared.Return(timestamps);
        }

        var incomingETag = ctx.Request.Headers.IfNoneMatch.Count > 0 ? ctx.Request.Headers.IfNoneMatch[0] : null;

        var asBuildTime = etagGenerator.AssemblyBuildTimeTicks;
        var ltDigitCount = UlongUtils.DigitCount(ltValue);
        var suffix = options.Suffix(ctx);
        var fullLength = asBuildTime.Length + 1 + ltDigitCount + suffix.Length + (suffix.Length > 0 ? 1 : 0);

        if (incomingETag is not null && ETagEqual(fullLength, incomingETag, ltValue, asBuildTime, suffix))
        {
            ctx.Response.StatusCode = StatusCodes.Status304NotModified;
            logger.LogNotModified(incomingETag);
            return true;
        }

        ctx.Response.Headers.CacheControl = options.CacheControl;
        var etag = etagGenerator.BuildETag(fullLength, ltValue, suffix);
        ctx.Response.Headers.ETag = etag;
        logger.LogETagAdded(etag);
        return false;
    }

    private static bool ETagEqual(int fullLength, string inETag, ulong lTimestamp, string asBuildTime, string suffix)
    {
        var ltDigitCount = UlongUtils.DigitCount(lTimestamp);

        if (fullLength != inETag.Length)
            return false;

        var incomingETag = inETag.AsSpan();
        var rightEdge = asBuildTime.Length;
        var inAsBuildTime = incomingETag[..rightEdge];
        if (!inAsBuildTime.Equals(asBuildTime.AsSpan(), StringComparison.Ordinal))
            return false;

        var inTicks = incomingETag.Slice(++rightEdge, ltDigitCount);
        if (!inTicks.EqualsLong(lTimestamp))
            return false;

        rightEdge += ltDigitCount;
        if (rightEdge == incomingETag.Length)
            return true;

        var inSuffix = incomingETag[++rightEdge..];
        if (!inSuffix.Equals(suffix, StringComparison.Ordinal))
            return false;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ISourceOperations GetOperationsProvider(
        HttpContext ctx, ImmutableGlobalOptions opt, ISourceOperationsResolver resolver) =>
        opt.SourceOperations ?? opt.SourceOperationsFactory?.Invoke(ctx) ?? resolver.Resolve(opt.Source);
}
