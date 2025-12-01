using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
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
}
