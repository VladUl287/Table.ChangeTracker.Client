using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql.EFCore.Tracker.AspNet.Services.Contracts;
using System.Runtime.CompilerServices;

namespace Npgsql.EFCore.Tracker.AspNet.Middlewares;

public sealed class TrackMiddleware<TContext>(
    RequestDelegate next, IETagService etagService, IActionsRegistry actionsRegistry, IPathResolver pathResolver) where TContext : DbContext
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsGetMethod(context))
        {
            var path = pathResolver.ResolvePath(context);
            var descriptor = actionsRegistry.GetActionDescriptor(path);

            var cancellationToken = context.RequestAborted;
            if (await etagService.ValidateAndSetETagAsync(context, descriptor, cancellationToken))
                return;
        }

        await next(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsGetMethod(HttpContext context) => context.Request.Method == HttpMethod.Get.Method;
}
