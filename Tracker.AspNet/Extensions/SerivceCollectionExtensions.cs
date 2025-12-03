using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Middlewares;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Extensions;

public static class SerivceCollectionExtensions
{
    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        return services.AddTracker<TContext>(new GlobalOptions());
    }

    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services, GlobalOptions options)
         where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));


        services.AddSingleton((provider) =>
        {
            if (options.Entities is { Length: > 0 })
            {
                var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
                AssignEntitiesToTables<TContext>(scopeFactory, options);
            }
            return options;
        });

        services.AddSingleton<IETagGenerator, ETagGenerator>();
        services.AddSingleton<IETagService, ETagService<TContext>>();

        services.AddSingleton<IRequestFilter, DefaultRequestFilter>();

        return services;
    }

    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services, Action<GlobalOptions> configure)
         where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new GlobalOptions();
        configure(options);
        return services.AddTracker<TContext>(options);
    }

    public static IApplicationBuilder UseTracker<TContext>(this IApplicationBuilder builder)
        where TContext : DbContext
    {
        return builder.UseMiddleware<TrackerMiddleware>();
    }

    public static IApplicationBuilder UseTracker<TContext>(this IApplicationBuilder builder, GlobalOptions options)
    where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        if (options.Entities is { Length: > 0 })
        {
            var scopeFactory = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            AssignEntitiesToTables<TContext>(scopeFactory, options);
        }

        return builder.UseMiddleware<TrackerMiddleware>(options);
    }

    private static void AssignEntitiesToTables<TContext>(IServiceScopeFactory scopeFactory, GlobalOptions options) where TContext : DbContext
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var tablesNames = dbContext.GetTablesNames(options.Entities);
        options.Tables = new HashSet<string>([.. options.Tables, .. tablesNames]).ToArray();
    }

    public static IApplicationBuilder UseTracker<TContext>(this IApplicationBuilder builder, Action<GlobalOptions> configure)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new GlobalOptions();
        configure(options);
        return builder.UseTracker<TContext>(options);
    }
}
