using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.FastEndpoints.Attributes;

namespace Tracker.FastEndpoints;

public sealed class TrackerPreProcessor<TRequest> : IPreProcessor<TRequest>
{
    private static ConcurrentDictionary<string, ImmutableGlobalOptions> _actionsOptions = new();

    public async Task PreProcessAsync(IPreProcessorContext<TRequest> ctx, CancellationToken ct)
    {
        var httpCtx = ctx.HttpContext;

        var endpoint = httpCtx.GetEndpoint();
        var options = GetOptions(httpCtx, endpoint.DisplayName);

        var service = httpCtx.Resolve<IRequestHandler>();
        var filter = httpCtx.Resolve<IRequestFilter>();

        if (filter.ValidRequest(httpCtx, options) && await service.HandleRequest(httpCtx, options, ct))
        {
            await httpCtx.Response.SendResultAsync(Results.StatusCode(304));
            return;
        }
    }

    private static ImmutableGlobalOptions GetOptions(HttpContext ctx, string key)
    {
        return _actionsOptions.GetOrAdd(key, (key, state) =>
        {
            var endpoint = ctx.GetEndpoint();

            var attribute = endpoint?.Metadata
                .GetOrderedMetadata<EndpointDefinition>()[0]
                .EndpointType
                .GetCustomAttribute<TrackPreProcessorAttribute>() ?? throw new InvalidOperationException();

            var scopeFactory = state.RequestServices.GetRequiredService<IServiceScopeFactory>();

            using var scope = scopeFactory.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<ImmutableGlobalOptions>();

            return options with
            {
                Tables = ResolveTables(attribute.Tables, options),
                ProviderId = attribute.ProviderId ?? options.ProviderId,
                CacheControl = attribute.CacheControl ?? options.CacheControl,
            };
        }, (ctx));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ImmutableArray<string> ResolveTables(IReadOnlyList<string>? tables, ImmutableGlobalOptions options) =>
        new HashSet<string>(tables ?? [.. options.Tables]).ToImmutableArray();
}
