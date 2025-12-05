using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public class ETagService(
    IETagGenerator etagGenerator, ISourceOperationsResolver operationsResolver, ILogger<ETagService> logger) : IETagService
{
    public async Task<bool> TrySetETagAsync(HttpContext context, ImmutableGlobalOptions options, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var sourceOperations = options.SourceOperations ?? 
            options.SourceOperationsFactory?.Invoke(context) ?? 
            operationsResolver.Resolve(options.Source);

        var suffix = options.Suffix(context);
        var etag = await GetETag(suffix, options, sourceOperations, token);
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
        context.Response.Headers.CacheControl = options.CacheControl;
        return false;
    }

    private async Task<string?> GetETag(string suffix, ImmutableGlobalOptions options, ISourceOperations dbOpeartions, CancellationToken token)
    {
        if (options is { Tables.Length: 0 })
        {
            var xact = await dbOpeartions.GetLastTimestamp(token);
            if (xact is null)
            {
                logger.LogLastTimestampNotFound();
                return null;
            }
            return etagGenerator.GenerateETag(xact.Value, suffix);
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

        return etagGenerator.GenerateETag([.. timestamps], suffix);
    }
}
