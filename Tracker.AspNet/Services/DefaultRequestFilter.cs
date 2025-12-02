using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class DefaultRequestFilter(ILogger<DefaultRequestFilter> logger) : IRequestFilter
{
    public bool ShouldProcessRequest<TState>(HttpContext context, Func<TState, GlobalOptions> optionsProvider, TState state)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            logger.LogNotGetRequest(context.Request.Method, context.Request.Path);
            return false;
        }

        if (context.Response.Headers.ETag.Count > 0)
        {
            logger.LogEtagHeaderPresent(context.Request.Path);
            return false;
        }

        if (HasImmutableCacheControl(context.Response.Headers.CacheControl))
        {
            logger.LogImmutableCacheDetected(context.Request.Path);
            return false;
        }

        var options = optionsProvider(state);
        if (!options.Filter(context))
        {
            logger.LogFilterRejected(context.Request.Path);
            return false;
        }

        logger.LogRequestValidated(context.Request.Path);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasImmutableCacheControl(StringValues cacheControlHeaders)
    {
        if (cacheControlHeaders.Count == 0)
            return false;

        const string IMMUTABLE = "immutable";
        foreach (var header in cacheControlHeaders)
        {
            if (header is not null && header.Contains(IMMUTABLE))
                return true;
        }

        return false;
    }
}
