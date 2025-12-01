using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tracker.AspNet.Filters;
using Tracker.Core.Extensions;

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
            var scopeFactory = provider.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var tablesNames = dbContext.GetTablesNames(entities);
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ETagEndpointFilter>>();
            var filter = new ETagEndpointFilter(tablesNames);
            return (context) =>
            {
                return filter.InvokeAsync(context, next);
            };
        });
    }
}
