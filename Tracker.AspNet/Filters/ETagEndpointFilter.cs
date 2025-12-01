using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Filters;

public sealed class ETagEndpointFilter() : IEndpointFilter
{
    public ETagEndpointFilter(GlobalOptions options) : this()
    {
        Options = options;
    }
    
    public GlobalOptions? Options { get; }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.IsGetRequest())
            return await next(context);

        var etagService = context.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var token = context.HttpContext.RequestAborted;

        var tables = Options?.Tables ?? [];
        var shouldReturnNotModified = await etagService.TrySetETagAsync(context.HttpContext, tables, token);
        if (shouldReturnNotModified)
            return Results.StatusCode(StatusCodes.Status304NotModified);

        return await next(context);
    }
}
