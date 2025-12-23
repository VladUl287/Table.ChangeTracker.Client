using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using Tracker.AspNet.Models;
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

            _actionOptions = options with
            {
                Source = sourceId ?? typeof(TContext).FullName ?? options.Source,
                CacheControl = cacheControl ?? options.CacheControl,
                Tables = ResolveTables(tables, entities, serviceProvider, options),
            };

            return _actionOptions;
        }
    }

    private static ImmutableArray<string> ResolveTables(
        string[]? tables, Type[]? entities, IServiceProvider services, ImmutableGlobalOptions options)
    {
        var tablesNames = new HashSet<string>(tables ?? []);

        if (entities is { Length: > 0 })
        {
            var dbContext = services.GetRequiredService<TContext>();
            foreach (var tableName in dbContext.GetTablesNames(entities))
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
