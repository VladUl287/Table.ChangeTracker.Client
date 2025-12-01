using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tracker.AspNet.Middlewares;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Extensions;

public static class SerivceCollectionExtensions
{
    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services, params Assembly[] assemblies)
         where TContext : DbContext
    {
        services.AddSingleton<IETagGenerator, ETagGenerator>();
        services.AddSingleton<IETagService, ETagService<TContext>>();

        return services;
    }

    public static IApplicationBuilder UseTracker<TContext>(this IApplicationBuilder builder)
        where TContext : DbContext
    {
        return builder.UseMiddleware<TrackerMiddleware<TContext>>(new MiddlewareOptions());
    }

    public static IApplicationBuilder UseTracker<TContext>(this IApplicationBuilder builder, string[] tables)
        where TContext : DbContext
    {
        return builder.UseMiddleware<TrackerMiddleware<TContext>>(new MiddlewareOptions { Tables = tables });
    }

    public static IApplicationBuilder UseTracker<TContext>(this IApplicationBuilder builder, Type[] entities)
        where TContext : DbContext
    {
        var scopeFactory = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var tables = ResolveTables(dbContext, entities);
        return builder.UseMiddleware<TrackerMiddleware<TContext>>(new MiddlewareOptions { Tables = tables });
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
