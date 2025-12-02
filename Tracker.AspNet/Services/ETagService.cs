using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Services;

public class ETagService<TContext>(
    IETagGenerator etagGenerator, ILogger<ETagService<TContext>> logger) : IETagService where TContext : DbContext
{
    public async Task<bool> TrySetETagAsync(HttpContext context, GlobalOptions options, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        if (context.Response.Headers.ETag.Count != 0)
        {
            logger.LogETagAlreadyExists();
            return false;
        }

        var dbContext = context.RequestServices.GetService<TContext>();
        if (dbContext is null)
        {
            logger.LogDbContextNotFound(typeof(TContext).Name);
            return false;
        }

        var etag = await GenerateETag(options.Tables, dbContext, token);
        if (etag is null)
        {
            logger.LogLastTimestampNotFound();
            return false;
        }

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            logger.LogNotModified(etag);
            return true;
        }

        logger.LogETagAdded(etag);
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "no-cache";
        return false;
    }

    private async Task<string?> GenerateETag(string[] tables, TContext dbContext, CancellationToken token)
    {
        if (tables is null or { Length: 0 })
        {
            var xact = await dbContext.GetLastCommittedXact(token);
            if (xact is null)
            {
                logger.LogLastTimestampNotFound();
                return null;
            }
            return etagGenerator.GenerateETag(xact.Value);
        }

        var timestamps = new List<DateTimeOffset>(tables.Length);
        foreach (var table in tables)
        {
            var lastTimestamp = await dbContext.GetLastTimestamp(table, token);
            if (string.IsNullOrEmpty(lastTimestamp))
            {
                logger.LogLastTimestampNotFound();
                return null;
            }
            timestamps.Add(DateTimeOffset.Parse(lastTimestamp));
        }
        return etagGenerator.GenerateETag([.. timestamps]);
    }
}
