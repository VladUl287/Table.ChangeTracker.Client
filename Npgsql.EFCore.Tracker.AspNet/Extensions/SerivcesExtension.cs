using Microsoft.Extensions.DependencyInjection;
using Npgsql.EFCore.Tracker.AspNet.Attributes;
using Npgsql.EFCore.Tracker.AspNet.Models;
using Npgsql.EFCore.Tracker.AspNet.Services;
using Npgsql.EFCore.Tracker.AspNet.Services.Contracts;
using System.Reflection;

namespace Npgsql.EFCore.Tracker.AspNet.Extensions;

public static class SerivcesExtension
{
    public static IServiceCollection AddTrackerSupport(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<IActionsRegistry, DefaultActionsRegistry>(provider =>
        {
            var descriptors = GetActionsDescriptors(assemblies);
            return new DefaultActionsRegistry(descriptors);
        });

        return services;
    }

    private static ActionDescriptor[] GetActionsDescriptors(params Assembly[] assemblies)
    {
        var result = new List<ActionDescriptor>();
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();

            foreach (var typ in types)
            {
                var methods = typ.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    var trackAttr = method.GetCustomAttribute<TrackAttribute>();

                    if (trackAttr is not null)
                    {
                        result.Add(new ActionDescriptor
                        {
                            Route = trackAttr.Route,
                            Tables = trackAttr.Tables
                        });
                    }
                }
            }
        }

        return result.ToArray();
    }
}
