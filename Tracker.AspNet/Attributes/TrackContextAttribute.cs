using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using Tracker.AspNet.Logging;
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
            var sourceResolver = serviceProvider.GetRequiredService<IProviderResolver>();
            var logger = serviceProvider.GetRequiredService<ILogger<TrackAttribute<TContext>>>();

            _actionOptions = options with
            {
                CacheControl = cacheControl ?? options.CacheControl,
                SourceProvider = sourceResolver.SelectProvider<TContext>(sourceId, options),
                Tables = ResolveTables(ctx, tables, entities, serviceProvider, options, logger)
            };

            logger.LogOptionsBuilded(GetActionName(ctx));
            return _actionOptions;
        }
    }

    private static ImmutableArray<string> ResolveTables(
        ActionExecutingContext ctx, string[]? tables, Type[]? entities,
        IServiceProvider serviceProvider, ImmutableGlobalOptions options, ILogger<TrackAttribute<TContext>> logger)
    {
        var tablesNames = new HashSet<string>();

        foreach (var tableName in tables ?? [])
        {
            if (!tablesNames.Add(tableName))
                logger.LogTableNameDuplicated(tableName, GetActionName(ctx));
        }

        if (entities is { Length: > 0 })
        {
            var dbContext = serviceProvider.GetRequiredService<TContext>();
            foreach (var tableName in dbContext.GetTablesNames(entities))
            {
                if (!tablesNames.Add(tableName))
                    logger.LogTableNameDuplicated(tableName, GetActionName(ctx));
            }
        }

        if (tables is null && entities is null)
        {
            logger.LogInformation("");

            foreach (var tableName in options.Tables)
            {
                if (!tablesNames.Add(tableName))
                    logger.LogTableNameDuplicated(tableName, GetActionName(ctx));
            }
        }

        return [.. tablesNames];
    }
}
