using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Models;
using Tracker.Core.Extensions;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Services.Contracts;
using Tracker.AspNet.Utils;

namespace Tracker.AspNet.Services;

public sealed class DefaultOptionsBuilder(IServiceScopeFactory scopeFactory) : IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>
{
    private static readonly string _defaultCacheControl = new CacheControlBuilder().WithNoCache().Combine();

    public ImmutableGlobalOptions Build(GlobalOptions options)
    {
        return new ImmutableGlobalOptions
        {
            Suffix = options.Suffix,
            Filter = options.Filter,
            Tables = [.. options.Tables],
            ProviderId = options.ProviderId,
            SourceProvider = options.SourceProvider,
            CacheControl = ResolveCacheControl(options),
            SourceProviderFactory = options.SourceProviderFactory,
            InvalidRequestDirectives = options.InvalidRequestDirectives is not null ? [.. options.InvalidRequestDirectives] : [],
            InvalidResponseDirectives = options.InvalidResponseDirectives is not null ? [.. options.InvalidResponseDirectives] : [],
        };
    }

    public ImmutableGlobalOptions Build<TContext>(GlobalOptions options) where TContext : DbContext
    {
        using var scope = scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var tables = new HashSet<string>([
            .. options.Tables ?? [],
            .. context.GetTablesNames(options.Entities ?? [])
        ]);

        return new ImmutableGlobalOptions
        {
            Tables = [.. tables],
            Suffix = options.Suffix,
            Filter = options.Filter,
            ProviderId = options.ProviderId,
            SourceProvider = options.SourceProvider,
            CacheControl = ResolveCacheControl(options),
            SourceProviderFactory = options.SourceProviderFactory,
            InvalidRequestDirectives = options.InvalidRequestDirectives is not null ? [.. options.InvalidRequestDirectives] : [],
            InvalidResponseDirectives = options.InvalidResponseDirectives is not null ? [.. options.InvalidResponseDirectives] : [],
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ResolveCacheControl(GlobalOptions options) =>
        options.CacheControl ?? options.CacheControlBuilder?.Combine() ?? _defaultCacheControl;
}
