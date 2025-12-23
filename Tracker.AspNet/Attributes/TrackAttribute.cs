using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Models;

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
            var options = serviceProvider.GetRequiredService<ImmutableGlobalOptions>();

            _actionOptions = options with
            {
                Source = sourceId ?? options.Source,
                Tables = ResolveTables(tables, options),
                CacheControl = cacheControl ?? options.CacheControl,
            };

            return _actionOptions;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ImmutableArray<string> ResolveTables(string[]? tables, ImmutableGlobalOptions options) =>
        new HashSet<string>(tables ?? [.. options.Tables]).ToImmutableArray();
}