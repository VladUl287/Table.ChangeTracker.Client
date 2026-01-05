using Tracker.AspNet.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

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

    public override ImmutableGlobalOptions GetOptions(HttpContext ctx)
    {
        if (_actionOptions is not null)
            return _actionOptions;

        lock (_lock)
        {
            if (_actionOptions is not null)
                return _actionOptions;

            var scopeFactory = ctx.RequestServices.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();

            var options = scope.ServiceProvider.GetRequiredService<ImmutableGlobalOptions>();

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
    private static ImmutableArray<string> ResolveTables(IReadOnlyList<string>? tables, ImmutableGlobalOptions options)
    {
        if (tables is null || tables.Count == 0)
            return options.Tables;

        return new HashSet<string>([.. tables, .. options.Tables])
            .ToImmutableArray();
    }
}