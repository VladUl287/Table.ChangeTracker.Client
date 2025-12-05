using Tracker.AspNet.Models;
using Tracker.Core.Extensions;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Tracker.AspNet.Services.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Tracker.AspNet.Services;

public sealed class GlobalOptionsBuilder(IServiceScopeFactory scopeFactory) : IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>
{
    public ImmutableGlobalOptions Build(GlobalOptions options)
    {
        var cacheControl = options.CacheControl ?? options.CacheControlBuilder?.Build();

        return new ImmutableGlobalOptions
        {
            Source = options.Source,
            Suffix = options.Suffix,
            Filter = options.Filter,
            CacheControl = cacheControl,
            Tables = [.. options.Tables],
            SourceOperations = options.SourceOperations,
            XactCacheLifeTime = options.XactCacheLifeTime,
            TablesCacheLifeTime = options.TablesCacheLifeTime,
            SourceOperationsFactory = options.SourceOperationsFactory,
        };
    }

    public ImmutableGlobalOptions Build<TContext>(GlobalOptions options) where TContext : DbContext
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var tablesNames = dbContext.GetTablesNames(options.Entities);
        var tables = new HashSet<string>([.. options.Tables, .. tablesNames]).ToImmutableArray();

        var source = options.Source;
        if (string.IsNullOrEmpty(source) && options is { SourceOperations: null, SourceOperationsFactory: null })
            source = typeof(TContext).GetTypeHashId();

        var cacheControl = options.CacheControl ?? options.CacheControlBuilder?.Build();

        return new ImmutableGlobalOptions
        {
            Tables = tables,
            Source = source,
            Filter = options.Filter,
            Suffix = options.Suffix,
            CacheControl = cacheControl,
            SourceOperations = options.SourceOperations,
            XactCacheLifeTime = options.XactCacheLifeTime,
            TablesCacheLifeTime = options.TablesCacheLifeTime,
            SourceOperationsFactory = options.SourceOperationsFactory,
        };
    }
}
