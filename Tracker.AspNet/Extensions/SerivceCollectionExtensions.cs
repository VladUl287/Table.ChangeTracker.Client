using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Extensions;

public static class SerivceCollectionExtensions
{
    public static IServiceCollection AddTracker(this IServiceCollection services) =>
        services.AddTracker(new GlobalOptions());

    public static IServiceCollection AddTracker(this IServiceCollection services, GlobalOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        services.AddSingleton((provider) =>
        {
            var optionsBuilder = provider.GetRequiredService<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
            return optionsBuilder.Build(options);
        });

        return services.AddTrackerBase();
    }

    public static IServiceCollection AddTracker(this IServiceCollection services, Action<GlobalOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new GlobalOptions();
        configure(options);
        return services.AddTracker(options);
    }

    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services) where TContext : DbContext =>
        services.AddTracker<TContext>(new GlobalOptions());

    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services, GlobalOptions options)
         where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        services.AddSingleton((provider) =>
        {
            var optionsBuilder = provider.GetRequiredService<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
            return optionsBuilder.Build<TContext>(options);
        });

        return services.AddTrackerBase();
    }

    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services, Action<GlobalOptions> configure)
         where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new GlobalOptions();
        configure(options);
        return services.AddTracker<TContext>(options);
    }

    private static IServiceCollection AddTrackerBase(this IServiceCollection services)
    {
        services.AddSingleton<ITrackerHasher, DefaultTrackerHasher>();

        services.AddSingleton<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>, GlobalOptionsBuilder>();

        services.AddSingleton<IAssemblyTimestampProvider>(new AssemblyTimestampProvider(Assembly.GetExecutingAssembly()));
        services.AddSingleton<IETagProvider, DefaultETagProvider>();

        services.AddSingleton<IRequestHandler, DefaultRequestHandler>();

        services.AddSingleton<IRequestFilter, DefaultRequestFilter>();

        return services;
    }
}
