using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.FastEndpoints;

public sealed class TrackerPreProcessor<TRequest> : IPreProcessor<TRequest>
{
    public async Task PreProcessAsync(IPreProcessorContext<TRequest> ctx, CancellationToken ct)
    {
        var httpCtx = ctx.HttpContext;

        var service = httpCtx.Resolve<IRequestHandler>();
        var filter = httpCtx.Resolve<IRequestFilter>();
        var opts = httpCtx.Resolve<ImmutableGlobalOptions>();

        if (filter.ValidRequest(httpCtx, opts) && await service.HandleRequest(httpCtx, opts, ct))
        {
            await httpCtx.Response.SendResultAsync(Results.StatusCode(304));
            return;
        }
    }
}
