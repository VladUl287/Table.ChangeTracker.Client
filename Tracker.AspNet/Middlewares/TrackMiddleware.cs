using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Middlewares;

public sealed class TrackMiddleware<TContext>(
    RequestDelegate next, IETagService etagService, IActionsRegistry actionsRegistry, IPathResolver pathResolver,
    ILogger<TrackMiddleware<TContext>> logger) where TContext : DbContext
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsGetMethod(context))
        {
            var path = pathResolver.ResolvePath(context);
            var descriptor = actionsRegistry.GetActionDescriptor(path);

            if (descriptor is not null)
            {
                var token = context.RequestAborted;
                if (await etagService.TrySetETagAsync(context, descriptor, token))
                    return;
            }
            else
                logger.LogDescriptorNotFound();
        }

        await next(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsGetMethod(HttpContext context) => context.Request.Method == HttpMethod.Get.Method;
}
