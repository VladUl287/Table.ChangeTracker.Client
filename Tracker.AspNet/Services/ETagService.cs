using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public class ETagService(
    IETagGenerator etagGenerator, IDbOperationsFactory dbOperationsFactory,
    ILogger<ETagService> logger) : IETagService
{
    public async Task<bool> TrySetETagAsync(HttpContext context, ImmutableGlobalOptions options, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var etag = await GenerateETag(options, token);
        if (etag is null)
        {
            logger.LogLastTimestampNotFound();
            return false;
        }

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            logger.LogNotModified(etag);
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return true;
        }

        logger.LogETagAdded(etag);
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "no-cache";
        return false;
    }

    public async Task<bool> TrySetETagAsync<TContext>(HttpContext context, ImmutableGlobalOptions options, CancellationToken token)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var etag = await GenerateETag<TContext>(options, token);
        if (etag is null)
        {
            logger.LogLastTimestampNotFound();
            return false;
        }

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            logger.LogNotModified(etag);
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return true;
        }

        logger.LogETagAdded(etag);
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "no-cache";
        return false;
    }

    private Task<string?> GenerateETag(ImmutableGlobalOptions options, CancellationToken token)
    {
        var dbOpeartions = dbOperationsFactory.Create(options.Provider);
        return GetETag(options, dbOpeartions, token);
    }

    private Task<string?> GenerateETag<TContext>(ImmutableGlobalOptions options, CancellationToken token)
        where TContext : DbContext
    {
        var dbOpeartions = dbOperationsFactory.Create<TContext>(options.Provider);
        return GetETag(options, dbOpeartions, token);
    }

    private async Task<string?> GetETag(ImmutableGlobalOptions options, IDbOperations dbOpeartions, CancellationToken token)
    {
        if (options is { Tables.Length: 0 })
        {
            var xact = await dbOpeartions.GetLastCommittedXact(token);
            if (xact is null)
            {
                logger.LogLastTimestampNotFound();
                return null;
            }
            return etagGenerator.GenerateETag(xact.Value);
        }

        var timestamps = new List<DateTimeOffset>(options.Tables.Length);
        foreach (var table in options.Tables)
        {
            var lastTimestamp = await dbOpeartions.GetLastTimestamp(table, token);
            if (lastTimestamp is null)
            {
                logger.LogLastTimestampNotFound();
                return null;
            }
            timestamps.Add(lastTimestamp.Value);
        }

        return etagGenerator.GenerateETag([.. timestamps]);
    }
}
