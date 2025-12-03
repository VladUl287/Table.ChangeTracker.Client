using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute : Attribute, IAsyncActionFilter
{
    private static readonly ConcurrentDictionary<string, ImmutableGlobalOptions> _optionsCache = new();
    private readonly ImmutableArray<string> _tables;

    public TrackAttribute()
    { }

    public TrackAttribute(params string[] tables)
    {
        ArgumentNullException.ThrowIfNull(tables, nameof(tables));
        _tables = [.. tables];
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext execContext, ActionExecutionDelegate next)
    {
        var httpCtx = execContext.HttpContext;

        var actionId = execContext.ActionDescriptor.Id;
        var options = _optionsCache.GetOrAdd(actionId,
            (key, state) =>
            {
                var baseOptions = state.httpCtx.RequestServices.GetRequiredService<ImmutableGlobalOptions>();
                return baseOptions with { Tables = _tables };
            },
            (httpCtx, _tables));

        var requestFilter = httpCtx.RequestServices.GetRequiredService<IRequestFilter>();
        var shouldProcessRequest = requestFilter.ShouldProcessRequest(httpCtx, options);
        if (!shouldProcessRequest)
        {
            await next();
            return;
        }

        var etagService = execContext.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var token = execContext.HttpContext.RequestAborted;

        var shouldReturnNotModified = await etagService.TrySetETagAsync(httpCtx, options, token);
        if (!shouldReturnNotModified)
        {
            await next();
            return;
        }
    }
}