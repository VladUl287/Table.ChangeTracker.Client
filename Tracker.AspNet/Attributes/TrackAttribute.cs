using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute() : Attribute, IAsyncActionFilter
{
    public TrackAttribute(string[] tables) : this()
    {
        ArgumentNullException.ThrowIfNull(tables, nameof(tables));
        Tables = tables;
    }

    public string[] Tables { get; } = [];

    public async Task OnActionExecutionAsync(ActionExecutingContext execContext, ActionExecutionDelegate next)
    {
        static bool filter(HttpContext ctx) => ctx.RequestServices.GetRequiredService<GlobalOptions>().Filter(ctx);

        var context = execContext.HttpContext;

        var requestFilter = context.RequestServices.GetRequiredService<IRequestFilter>();
        var shouldProcessRequest = requestFilter.ShouldProcessRequest(context, filter);
        if (!shouldProcessRequest)
        {
            await next();
            return;
        }

        var options = context.RequestServices.GetRequiredService<GlobalOptions>();
        options = options.Copy();
        options.Tables = Tables;

        var etagService = execContext.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var token = execContext.HttpContext.RequestAborted;

        var shouldReturnNotModified = await etagService.TrySetETagAsync(execContext.HttpContext, options, token);
        if (shouldReturnNotModified)
        {
            execContext.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
            return;
        }

        await next();
    }
}