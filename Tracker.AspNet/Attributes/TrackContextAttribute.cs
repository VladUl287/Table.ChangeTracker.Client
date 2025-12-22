using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;
using Tracker.Core.Services.Contracts;

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
            var sourceIdGenerator = serviceProvider.GetRequiredService<IProviderIdGenerator>();
            var logger = serviceProvider.GetRequiredService<ILogger<TrackAttribute<TContext>>>();

            var sourceOperations = sourceResolver.SelectProvider<TContext>(sourceId, options);

            cacheControl ??= options.CacheControl;

            _actionOptions = options with
            {
                CacheControl = cacheControl,
                SourceProvider = sourceOperations,
                Tables = GetAndCombineTablesNames(ctx, tables, entities, serviceProvider, logger)
            };
            logger.LogOptionsBuilded(GetActionName(ctx));
            return _actionOptions;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetActionName(ActionExecutingContext ctx) =>
        ctx.ActionDescriptor.DisplayName ?? ctx.ActionDescriptor.Id;

    private static ImmutableArray<string> GetAndCombineTablesNames(ActionExecutingContext ctx,
        string[]? tables, Type[]? entities, IServiceProvider serviceProvider, ILogger<TrackAttribute<TContext>> logger)
    {
        var tablesNames = new HashSet<string>();
        foreach (var tableName in tables ?? [])
            if (!tablesNames.Add(tableName))
                logger.LogTableNameDuplicated(tableName, GetActionName(ctx));

        if (entities is { Length: > 0 })
        {
            var dbContext = serviceProvider.GetRequiredService<TContext>();
            foreach (var tableName in dbContext.GetTablesNames(entities))
                if (!tablesNames.Add(tableName))
                    logger.LogTableNameDuplicated(tableName, GetActionName(ctx));
        }

        return [.. tablesNames];
    }
}
