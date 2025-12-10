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

        services.AddSingleton<ITimestampsHasher, XxHash64Hasher>();

        services.AddSingleton<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>, GlobalOptionsBuilder>();

        services.AddSingleton((provider) =>
        {
            var optionsBuilder = provider.GetRequiredService<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
            return optionsBuilder.Build(options);
        });

        services.AddSingleton<IETagService>(new ETagService(Assembly.GetExecutingAssembly()));
        services.AddSingleton<IRequestHandler, RequestHandler>();

        services.AddSingleton<ISourceOperationsResolver, SourceOperationsResolver>();

        services.AddSingleton<IRequestFilter, DefaultRequestFilter>();

        services.AddSingleton<IStartupFilter, SourceOperationsValidator>();

        return services;
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

        services.AddSingleton<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>, GlobalOptionsBuilder>();

        services.AddSingleton((provider) =>
        {
            var optionsBuilder = provider.GetRequiredService<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
            return optionsBuilder.Build<TContext>(options);
        });

        services.AddSingleton<IETagService>(new ETagService(Assembly.GetExecutingAssembly()));
        services.AddSingleton<IRequestHandler, RequestHandler>();

        services.AddSingleton<ISourceOperationsResolver, SourceOperationsResolver>();

        services.AddSingleton<IRequestFilter, DefaultRequestFilter>();

        services.AddSingleton<IStartupFilter, SourceOperationsValidator>();

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
}
