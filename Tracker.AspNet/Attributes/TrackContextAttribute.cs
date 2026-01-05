using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using Tracker.AspNet.Models;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TrackAttribute<TContext>(
    string[]? tables = null,
    Type[]? entities = null,
    string? providerId = null,
    string? cacheControl = null) : TrackAttribute(tables, providerId, cacheControl) where TContext : DbContext
{
    public IReadOnlyList<Type>? Entities => entities;

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
            var tableNameResolver = serviceProvider.GetRequiredService<ITableNameResolver>();
            var options = serviceProvider.GetRequiredService<ImmutableGlobalOptions>();

            _actionOptions = options with
            {
                ProviderId = ProviderId ?? typeof(TContext).FullName ?? options.ProviderId,
                CacheControl = CacheControl ?? options.CacheControl,
                Tables = ResolveTables(Tables, entities, serviceProvider, tableNameResolver, options),
            };

            return _actionOptions;
        }
    }

    private static ImmutableArray<string> ResolveTables(
        IReadOnlyList<string>? tables, Type[]? entities, IServiceProvider services, ITableNameResolver tableNameResolver, ImmutableGlobalOptions options)
    {
        var tablesNames = new HashSet<string>(tables ?? []);

        if (entities is { Length: > 0 })
        {
            var dbContext = services.GetRequiredService<TContext>();
            foreach (var tableName in tableNameResolver.GetTablesNames(dbContext, entities))
                tablesNames.Add(tableName);
        }

        if (tables is null && entities is null)
        {
            foreach (var tableName in options.Tables)
                tablesNames.Add(tableName);
        }

        return [.. tablesNames];
    }
}
