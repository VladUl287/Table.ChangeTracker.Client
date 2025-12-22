using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Utils;
using Tracker.Core.Extensions;
using Tracker.Core.Services.Contracts;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class GlobalOptionsBuilder(IServiceScopeFactory scopeFactory) : IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>
{
    private static readonly string _defaultCacheControl = new CacheControlBuilder().WithNoCache().Combine();

    public ImmutableGlobalOptions Build(GlobalOptions options)
    {
        using var scope = scopeFactory.CreateScope();

        var providerSelector = scope.ServiceProvider.GetRequiredService<IProviderResolver>();

        return new ImmutableGlobalOptions
        {
            SourceProvider = providerSelector.SelectProvider(options),
            SourceProviderFactory = options.SourceProviderFactory,

            Suffix = options.Suffix,
            Filter = options.Filter,
            InvalidRequestDirectives = [.. options.InvalidRequestDirectives],
            InvalidResponseDirectives = [.. options.InvalidResponseDirectives],
            Tables = [.. options.Tables],
            CacheControl = ResolveCacheControl(options),
        };
    }

    public ImmutableGlobalOptions Build<TContext>(GlobalOptions options) where TContext : DbContext
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var sourceIdGenerator = scope.ServiceProvider.GetRequiredService<IProviderIdGenerator>();
        var providerSelector = scope.ServiceProvider.GetRequiredService<IProviderResolver>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GlobalOptionsBuilder>>();

        var tables = GetAndCombineTablesNames(options, dbContext, logger);

        return new ImmutableGlobalOptions
        {
            SourceProvider = providerSelector.SelectProvider<TContext>(options),
            SourceProviderFactory = options.SourceProviderFactory,

            Tables = tables,
            Suffix = options.Suffix,
            Filter = options.Filter,
            InvalidRequestDirectives = [.. options.InvalidRequestDirectives],
            InvalidResponseDirectives = [.. options.InvalidResponseDirectives],
            CacheControl = ResolveCacheControl(options),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ResolveCacheControl(GlobalOptions options) =>
        options.CacheControl ?? options.CacheControlBuilder?.Combine() ?? _defaultCacheControl;

    private static ImmutableArray<string> GetAndCombineTablesNames<TContext>(
        GlobalOptions options, TContext dbContext, ILogger<GlobalOptionsBuilder> logger) where TContext : DbContext
    {
        var tablesNames = new HashSet<string>();
        foreach (var tableName in options.Tables ?? [])
            if (!tablesNames.Add(tableName))
                logger.LogOptionsTableNameDuplicated(tableName);

        foreach (var tableName in dbContext.GetTablesNames(options.Entities ?? []))
            if (!tablesNames.Add(tableName))
                logger.LogOptionsTableNameDuplicated(tableName);

        return [.. tablesNames];
    }
}
