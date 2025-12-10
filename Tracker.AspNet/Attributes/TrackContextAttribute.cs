using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute<TContext>(
    string[]? tables = null,
    Type[]? entities = null,
    string? sourceId = null,
    string? cacheControl = null) : TrackAttributeBase where TContext : DbContext
{
    private ImmutableGlobalOptions? _actionOptions;
    private readonly Lock _lock = new();

    protected override ImmutableGlobalOptions GetOptions(ActionExecutingContext execCtx)
    {
        if (_actionOptions is not null)
            return _actionOptions;

        lock (_lock)
        {
            if (_actionOptions is not null)
                return _actionOptions;

            var scopeFactory = execCtx.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();

            var serviceProvider = scope.ServiceProvider;
            var options = serviceProvider.GetRequiredService<ImmutableGlobalOptions>();
            var opResolver = serviceProvider.GetRequiredService<ISourceOperationsResolver>();
            var logger = serviceProvider.GetRequiredService<ILogger<TrackAttribute<TContext>>>();

            var tablesNames = GetAndCombineTablesNames(tables, entities, serviceProvider, logger);

            cacheControl ??= options.CacheControl;
            if (sourceId is null)
            {
                var dbHashId = typeof(TContext).GetTypeHashId();
                if (opResolver.Registered(dbHashId))
                {
                    sourceId = dbHashId;
                    logger.LogInformation("Source id {sourceId} taked from TContext type hash due param source id is null.", sourceId);
                }
                else
                {
                    sourceId = options.Source;
                    logger.LogInformation("Source id {sourceId} taked from options due TContext not registered.", sourceId);
                }
            }

            return _actionOptions = options with
            {
                Source = sourceId,
                CacheControl = cacheControl,
                Tables = tablesNames
            };
        }
    }

    private static ImmutableArray<string> GetAndCombineTablesNames(
        string[]? tables, Type[]? entities, IServiceProvider serviceProvider, ILogger<TrackAttribute<TContext>> logger)
    {
        var tablesNames = new HashSet<string>();
        foreach (var tableName in tables ?? [])
        {
            if (!tablesNames.Add(tableName))
                logger.LogWarning("Table name duplicated. It will be skipped");
        }

        if (entities is { Length: > 0 })
        {
            logger.LogInformation("Start resolve tables names from context");

            var dbContext = serviceProvider.GetRequiredService<TContext>();
            var entitiesTablesNames = dbContext.GetTablesNames(entities ?? []);
            foreach (var table in entitiesTablesNames)
            {
                if (!tablesNames.Add(table))
                    logger.LogWarning("Entity table name duplicated. It will be skipped");
            }
        }

        return [.. tablesNames];
    }
}
