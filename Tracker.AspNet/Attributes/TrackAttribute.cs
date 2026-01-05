using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TrackAttribute(
    string[]? tables = null,
    string? providerId = null,
    string? cacheControl = null) : TrackAttributeBase
{
    protected private ImmutableGlobalOptions? _actionOptions;

#if NET9_0_OR_GREATER
    protected private readonly Lock _lock = new();
#else
    protected private readonly object _lock = new();
#endif

    public IReadOnlyList<string>? Tables => tables;
    public string? ProviderId => providerId;
    public string? CacheControl => cacheControl;

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
                ProviderId = ProviderId ?? options.ProviderId,
                Tables = ResolveTables(Tables, options),
                CacheControl = CacheControl ?? options.CacheControl,
            };

            return _actionOptions;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ImmutableArray<string> ResolveTables(IReadOnlyList<string>? tables, ImmutableGlobalOptions options) =>
        new HashSet<string>(tables ?? [.. options.Tables]).ToImmutableArray();
}