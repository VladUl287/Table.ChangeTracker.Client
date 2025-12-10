using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

public abstract class TrackAttributeBase : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext execCtx, ActionExecutionDelegate next)
    {
        var options = GetOptions(execCtx);

        if (RequestValid(execCtx.HttpContext, options) && await NotModified(execCtx.HttpContext, options))
            return;

        await next();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool RequestValid(HttpContext httpCtx, ImmutableGlobalOptions options) =>
        httpCtx.RequestServices
            .GetRequiredService<IRequestFilter>()
            .RequestValid(httpCtx, options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task<bool> NotModified(HttpContext httpCtx, ImmutableGlobalOptions options) =>
        httpCtx.RequestServices
            .GetRequiredService<IRequestHandler>()
            .IsNotModified(httpCtx, options, httpCtx.RequestAborted);

    protected abstract ImmutableGlobalOptions GetOptions(ActionExecutingContext execContext);
}
