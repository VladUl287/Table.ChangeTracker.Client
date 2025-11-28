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
    public async ValueTask<bool> TrySetETagAsync(HttpContext context, ActionDescriptor descriptor, CancellationToken token = default)
    {
        if (context.Response.Headers.ContainsKey("ETag"))
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

        var lastTimestamp = await dbContext.GetLastTimestamp(descriptor.Tables, token);
        if (string.IsNullOrEmpty(lastTimestamp))
        {
            logger.LogLastTimestampNotFound();
            return false;
        }

        var dateTime = DateTimeOffset.Parse(lastTimestamp);
        var etag = etagGenerator.GenerateETag(dateTime);

        if (context.Request.Headers["If-None-Match"] == etag)
        {
            logger.LogNotModified(etag);
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return true;
        }

        logger.LogETagAdded(etag);
        context.Response.Headers["ETag"] = etag;
        return false;
    }
}
