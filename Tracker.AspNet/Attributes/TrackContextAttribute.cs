using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute<TContext> : Attribute, IAsyncActionFilter
    where TContext : DbContext
{
    private ImmutableGlobalOptions? _actionOptions;
    private readonly Lock _lock = new();
    private readonly ImmutableArray<string> _tables = [];

    public TrackAttribute(params string[] tables)
    {
        ArgumentNullException.ThrowIfNull(tables, nameof(tables));

        _tables = [.. tables];
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext execContext, ActionExecutionDelegate next)
    {
        var options = GetOrSetOptions(execContext);

        var httpCtx = execContext.HttpContext;
        var canceltoken = execContext.HttpContext.RequestAborted;

        var requestFilter = httpCtx.RequestServices.GetRequiredService<IRequestFilter>();
        var shouldProcessRequest = requestFilter.ShouldProcessRequest(httpCtx, options);
        if (!shouldProcessRequest)
        {
            await next();
            return;
        }

        if (canceltoken.IsCancellationRequested)
            return;

        var etagService = execContext.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var shouldReturnNotModified = await etagService.TrySetETagAsync(httpCtx, options, canceltoken);
        if (!shouldReturnNotModified)
        {
            await next();
            return;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ImmutableGlobalOptions GetOrSetOptions(ActionExecutingContext execContext)
    {
        if (_actionOptions is not null)
            return _actionOptions;

        lock (_lock)
        {
            if (_actionOptions is not null)
                return _actionOptions;

            var baseOptions = execContext.HttpContext.RequestServices.GetRequiredService<ImmutableGlobalOptions>();
            var source = typeof(TContext).GetTypeHashId();
            _actionOptions = baseOptions with
            {
                Source = source,
                Tables = _tables
            };
            return _actionOptions;
        }
    }
}
