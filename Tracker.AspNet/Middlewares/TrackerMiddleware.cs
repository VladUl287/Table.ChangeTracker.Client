using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Middlewares;

public sealed class TrackerMiddleware(RequestDelegate next, IETagService eTagService, GlobalOptions opts)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.IsGetRequest())
        {
            await next(context);
            return;
        }

        if (opts is not null)
        {
            if (opts.Filter is null || !opts.Filter(context))
            {
                await next(context);
                return;
            }
        }

        var token = context.RequestAborted;

        var shouldReturnNotModified = await eTagService.TrySetETagAsync(context, opts?.Tables ?? [], token);
        if (shouldReturnNotModified)
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        await next(context);
    }
}