using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Filters;

public sealed class ETagEndpointFilter() : IEndpointFilter
{
    public ETagEndpointFilter(string[] tables) : this()
    {
        Tables = tables;
    }

    public string[] Tables { get; } = [];

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.IsGetRequest())
            return await next(context);

        var etagService = context.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var token = context.HttpContext.RequestAborted;

        var shouldReturnNotModified = await etagService.TrySetETagAsync(context.HttpContext, Tables, token);
        if (shouldReturnNotModified)
            return Results.StatusCode(StatusCodes.Status304NotModified);

        return await next(context);
    }
}
