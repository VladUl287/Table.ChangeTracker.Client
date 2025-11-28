using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tracker.AspNet.Middlewares;
using Tracker.AspNet.Services;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Extensions;

public static class SerivceCollectionExtensions
{
    public static IApplicationBuilder UseTracker<TContext>(this IApplicationBuilder builder)
        where TContext : DbContext
    {
        return builder
            .UseMiddleware<TrackMiddleware<TContext>>();
    }

    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services, params Assembly[] assemblies)
         where TContext : DbContext
    {
        services.AddSingleton<IPathResolver, DefaultPathResolver>();
        services.AddSingleton<IETagGenerator, ETagGenerator>();
        services.AddSingleton<IETagService, ETagService<TContext>>();

        services.AddSingleton<IActionsDescriptorProvider, DefaultActionsDescriptorProvider>();
        services.AddSingleton<IActionsRegistry, DefaultActionsRegistry>(provider =>
        {
            var descriptorProvider = provider.GetRequiredService<IActionsDescriptorProvider>();
            var descriptors = descriptorProvider.GetActionsDescriptors(assemblies);
            return new DefaultActionsRegistry(descriptors);
        });

        return services;
    }
}
