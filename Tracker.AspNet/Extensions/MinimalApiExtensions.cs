using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Filters;
using Tracker.AspNet.Models;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Extensions;

public static class MinimalApiExtensions
{
    public static IEndpointConventionBuilder WithTracking(this IEndpointConventionBuilder endpoint)
    {
        return endpoint.AddEndpointFilter<IEndpointConventionBuilder, ETagEndpointFilter>();
    }

    public static IEndpointConventionBuilder WithTracking<TContext>(this IEndpointConventionBuilder endpoint, GlobalOptions options)
        where TContext : DbContext
    {
        return endpoint.AddEndpointFilterFactory((provider, next) =>
        {
            if (options.Entities is { Length: > 0 })
            {
                var scopeFactory = provider.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
                var tablesNames = dbContext.GetTablesNames(options.Entities);
                options.Tables = [.. options.Tables, .. tablesNames];
            }

            var filter = new ETagEndpointFilter(options);
            return (context) => filter.InvokeAsync(context, next);
        });
    }

    public static IEndpointConventionBuilder WithTracking<TContext>(this IEndpointConventionBuilder endpoint, Action<GlobalOptions> configure)
        where TContext : DbContext
    {
        var options = new GlobalOptions();
        configure(options);
        return endpoint.WithTracking<TContext>(options);
    }
}
