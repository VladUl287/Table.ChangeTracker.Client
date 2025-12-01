using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Middlewares;

public sealed class TrackerMiddleware<TContext>(
    RequestDelegate next, IETagService eTagService, MiddlewareOptions options) where TContext : DbContext
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.IsGetRequest())
        {
            var token = context.RequestAborted;

            var shouldReturnNotModified = await eTagService.TrySetETagAsync(context, options.Tables ?? [], token);
            if (shouldReturnNotModified)
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                return;
            }
        }

        await next(context);
    }
}