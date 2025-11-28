using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql.EFCore.Tracker.AspNet.Logging;
using Npgsql.EFCore.Tracker.AspNet.Services.Contracts;
using System.Runtime.CompilerServices;

namespace Npgsql.EFCore.Tracker.AspNet.Middlewares;

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
            var token = context.RequestAborted;

            if (descriptor is not null)
            {
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
