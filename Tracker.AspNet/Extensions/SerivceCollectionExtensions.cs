using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tracker.AspNet.Middlewares;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

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

    public static IApplicationBuilder UseTracker<TContext>(this IApplicationBuilder builder, Action<MiddlewareOptions> configure)
        where TContext : DbContext
    {
        var options = new MiddlewareOptions();
        configure(options);

        if (options.Entities is { Length: > 0 })
        {
            var scopeFactory = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var tablesNames = dbContext.GetTablesNames(options.Entities);
            options.Tables = [.. options.Tables, .. tablesNames];
        }

        return builder.UseMiddleware<TrackerMiddleware<TContext>>(options);
    }
}
