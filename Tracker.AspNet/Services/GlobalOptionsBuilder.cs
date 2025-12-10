using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;
using Tracker.Core.Services.Contracts;

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
            SourceOperationsFactory = options.SourceOperationsFactory,
        };
    }

    public ImmutableGlobalOptions Build<TContext>(GlobalOptions options) where TContext : DbContext
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var sourceIdGenerator = scope.ServiceProvider.GetRequiredService<ISourceIdGenerator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GlobalOptionsBuilder>>();

        var cacheControl = options.CacheControl ?? options.CacheControlBuilder?.Build();
        var tables = GetAndCombineTablesNames(options, dbContext, logger);

        return new ImmutableGlobalOptions
        {
            Tables = tables,
            Source = options.Source,
            Filter = options.Filter,
            Suffix = options.Suffix,
            CacheControl = cacheControl,
            SourceOperations = options.SourceOperations,
            SourceOperationsFactory = options.SourceOperationsFactory,
        };
    }

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
