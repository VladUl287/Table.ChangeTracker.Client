using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Middlewares;

public sealed class TrackerMiddleware(
    RequestDelegate next, IRequestFilter filter, IRequestHandler service,
    ImmutableGlobalOptions opts)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        if (filter.RequestValid(ctx, opts) && await service.IsNotModified(ctx, opts, ctx.RequestAborted))
            return;

        await next(ctx);
    }
}