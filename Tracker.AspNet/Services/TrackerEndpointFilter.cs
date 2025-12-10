using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class TrackerEndpointFilter(
    IRequestHandler service, IRequestFilter filter, ImmutableGlobalOptions opts) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext filterCtx, EndpointFilterDelegate next)
    {
        var httpCtx = filterCtx.HttpContext;

        if (filter.RequestValid(httpCtx, opts) && await service.IsNotModified(httpCtx, opts, httpCtx.RequestAborted))
            return Results.StatusCode(StatusCodes.Status304NotModified);

        return await next(filterCtx);
    }
}
