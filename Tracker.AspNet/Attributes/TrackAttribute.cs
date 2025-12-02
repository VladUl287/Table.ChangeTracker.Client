using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Logging;
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

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TrackAttribute>>();

        var httpContext = context.HttpContext;
        var request = httpContext.Request;

        if (!httpContext.IsGetRequest())
        {
            logger.LogInformation("Not 'GET' request but '{request}'. Ignore tracker logic.", context.HttpContext.Request.Method);
            await next();
            return;
        }

        var ifNoneMatch = request.Headers.IfNoneMatch;
        var ifModifiedSince = request.Headers.IfModifiedSince;

        if (ifNoneMatch.Count == 0 && ifModifiedSince.Count == 0)
        {
            //"No conditional headers (If-None-Match/If-Modified-Since)"
            return;
        }

        var cacheControl = request.Headers.CacheControl.ToString();
        if (!string.IsNullOrEmpty(cacheControl))
        {
            // Check for directives that prevent caching
            if (cacheControl.Contains("no-cache") ||
                cacheControl.Contains("no-store") ||
                cacheControl.Contains("must-revalidate") ||
                cacheControl.Contains("max-age=0"))
            {
                //$"Cache-Control prevents caching: {cacheControl}";
                return;
            }
        }

        var pragma = request.Headers.Pragma.ToString();
        if (pragma.Contains("no-cache"))
        {
            //$"Pragma: no-cache prevents caching";
            return;
        }

        if (httpContext.Response.Headers.ETag.Count != 0)
        {
            await next();
            return;
        }

        if (IsImmutableCache(httpContext.Response))
        {
            await next();
            return;
        }

        var options = context.HttpContext.RequestServices.GetRequiredService<GlobalOptions>();
        if (!options.Filter(context.HttpContext))
        {
            logger.LogInformation("Request '{request}' filtered by options. Ignore tracker logic.", context.HttpContext.Request.Method);
            await next();
            return;
        }

        options = options.Copy();
        options.Tables = Tables;

        var etagService = context.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var token = context.HttpContext.RequestAborted;

        var shouldReturnNotModified = await etagService.TrySetETagAsync(context.HttpContext, options, token);
        if (shouldReturnNotModified)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
            return;
        }

        await next();
    }

    static bool IsImmutableCache(HttpResponse response)
    {
        foreach (var header in response.Headers.CacheControl)
        {
            if (header is null)
            {
                continue;
            }

            if (header.Contains("immutable"))
            {
                return true;
            }
        }

        return false;
    }


    private bool ShouldSkip304BasedOnCacheControl(string cacheControl, string method, string path)
    {
        // Quick exit if no cache control
        if (string.IsNullOrEmpty(cacheControl))
            return false;

        // 1. Highest priority: no-store - definitely skip
        if (cacheControl.Contains("no-store"))
        {
            return true; // Skip entirely
        }

        // 2. no-cache - client wants to validate, we can still process 304
        // but might want to skip if you don't want to handle validation
        if (cacheControl.Contains("no-cache"))
        {
            // Optional: You could still process, but log differently
            // return false; // Still process
            return true; // Skip if you want simple implementation
        }

        // 3. Check max-age
        var maxAgeMatch = Regex.Match(cacheControl, @"max-age\s*=\s*(\d+)");
        if (maxAgeMatch.Success)
        {
            var maxAge = int.Parse(maxAgeMatch.Groups[1].Value);
            if (maxAge <= 0)
            {
                return true; // Skip
            }
        }

        return false;
    }
}