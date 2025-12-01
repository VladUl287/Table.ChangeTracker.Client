using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tracker.AspNet.Filters;

namespace Tracker.AspNet.Extensions;

public static class MinimalApiExtensions
{
    public static IEndpointConventionBuilder WithTracking(this IEndpointConventionBuilder endpoint)
    {
        return endpoint.AddEndpointFilter<IEndpointConventionBuilder, ETagEndpointFilter>();
    }

    public static IEndpointConventionBuilder WithTracking(this IEndpointConventionBuilder endpoint, string[] tables)
    {
        return endpoint.AddEndpointFilterFactory((provider, next) =>
        {
            var logger = provider.ApplicationServices.GetRequiredService<ILogger<ETagEndpointFilter>>();
            var filter = new ETagEndpointFilter(tables);
            return (context) => filter.InvokeAsync(context, next);
        });
    }

    public static IEndpointConventionBuilder WithTracking<TContext>(this IEndpointConventionBuilder endpoint, Type[] entities)
        where TContext : DbContext
    {
        return endpoint.AddEndpointFilterFactory((provider, next) =>
        {
            var dbContext = provider.ApplicationServices.GetRequiredService<TContext>();
            var tables = ResolveTables(dbContext, entities);
            var logger = provider.ApplicationServices.GetRequiredService<ILogger<ETagEndpointFilter>>();
            var filter = new ETagEndpointFilter(tables);
            return (context) => filter.InvokeAsync(context, next);
        });
    }

    private static string[] ResolveTables<TContext>(TContext context, Type[] entities)
         where TContext : DbContext
    {
        var result = new HashSet<string>();

        var contextModel = context.Model;

        foreach (var entity in entities ?? [])
        {
            var entityType = contextModel.FindEntityType(entity) ??
                throw new NullReferenceException($"Table entity type not found for type {entity.FullName}");

            var tableName = entityType.GetSchemaQualifiedTableName() ??
                throw new NullReferenceException($"Table entity type not found for type {entity.FullName}");

            result.Add(tableName);
        }

        return [.. result];
    }
}
