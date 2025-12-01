using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute : Attribute, IAsyncActionFilter
{
    public TrackAttribute()
    { }

    public TrackAttribute(string[] tables)
    {
        ArgumentNullException.ThrowIfNull(tables, nameof(tables));
        Tables = tables;
    }

    public TrackAttribute(Type[] entities)
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));
        Entities = entities;
    }

    public TrackAttribute(string[] tables, Type[] entities)
    {
        ArgumentNullException.ThrowIfNull(tables, nameof(tables));
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));
        Tables = tables;
        Entities = entities;
    }

    public string[] Tables { get; } = [];
    public Type[] Entities { get; } = [];

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (IsGetMethod(context.HttpContext))
        {
            var etagService = context.HttpContext.RequestServices.GetRequiredService<IETagService>();
            var token = context.HttpContext.RequestAborted;

            if (IsGlobalRequest())
            {
                if (await etagService.TrySetETagAsync(context.HttpContext, token))
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
                    return;
                }
            }
            else
            {
                if (await etagService.TrySetETagAsync(context.HttpContext, Tables, token))
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
                    return;
                }
            }
        }

        await next();
    }

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => Task.CompletedTask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsGlobalRequest() => Tables is null or { Length: 0 } && Entities is null or { Length: 0 };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsGetMethod(HttpContext context) => context.Request.Method == HttpMethod.Get.Method;
}