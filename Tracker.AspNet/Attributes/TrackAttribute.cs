using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute(params string[] tables) : Attribute, IAsyncActionFilter
{
    private readonly string[] _tables = tables ?? throw new NullReferenceException();
    private readonly string? _sourceId = null;
    private readonly string? _cacheControl = null;

    public TrackAttribute(string? sourceId = null, params string[] tables) : this(tables)
        => _sourceId = sourceId;

    public TrackAttribute(string? sourceId = null, string? cacheControl = null, params string[] tables) : this(sourceId, tables)
        => _cacheControl = cacheControl;

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

    private ImmutableGlobalOptions? _actionOptions;
    private readonly Lock _lock = new();

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
            _actionOptions = baseOptions with
            {
                CacheControl = _cacheControl ?? baseOptions.CacheControl,
                Source = _sourceId ?? baseOptions.Source,
                Tables = [.. _tables]
            };
            return _actionOptions;
        }
    }
}