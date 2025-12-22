using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute(
    string[]? tables = null,
    string? sourceId = null,
    string? cacheControl = null) : TrackAttributeBase
{
    private ImmutableGlobalOptions? _actionOptions;
    private readonly Lock _lock = new();

    protected internal override ImmutableGlobalOptions GetOptions(ActionExecutingContext ctx)
    {
        if (_actionOptions is not null)
            return _actionOptions;

        lock (_lock)
        {
            if (_actionOptions is not null)
                return _actionOptions;

            var scopeFactory = ctx.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();

            var serviceProvider = scope.ServiceProvider;
            var providerSelector = serviceProvider.GetRequiredService<IProviderResolver>();
            var options = serviceProvider.GetRequiredService<ImmutableGlobalOptions>();
            var logger = serviceProvider.GetRequiredService<ILogger<TrackAttribute>>();

            var operationsProvider = providerSelector.SelectProvider(sourceId, options);

            _actionOptions = options with
            {
                SourceProvider = operationsProvider,
                CacheControl = cacheControl ?? options.CacheControl,
                Tables = tables?.ToImmutableArray() ?? []
            };

            logger.LogOptionsBuilded(ctx.ActionDescriptor.DisplayName ?? ctx.ActionDescriptor.Id);
            return _actionOptions;
        }
    }
}