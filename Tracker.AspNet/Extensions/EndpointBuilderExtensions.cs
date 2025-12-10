using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Extensions;

public static class EndpointBuilderExtensions
{
    public static IEndpointConventionBuilder WithTracking(this IEndpointConventionBuilder endpoint)
    {
        return endpoint.AddEndpointFilter<IEndpointConventionBuilder, TrackerEndpointFilter>();
    }

    public static IEndpointConventionBuilder WithTracking<TContext>(this IEndpointConventionBuilder endpoint, GlobalOptions options)
        where TContext : DbContext
    {
        return endpoint.AddEndpointFilterFactory((provider, next) =>
        {
            var builder = provider.ApplicationServices.GetRequiredService<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
            var immutableOptions = builder.Build<TContext>(options);

            var etagService = provider.ApplicationServices.GetRequiredService<IRequestHandler>();
            var requestFilter = provider.ApplicationServices.GetRequiredService<IRequestFilter>();

            var filter = new TrackerEndpointFilter(etagService, requestFilter, immutableOptions);
            return (context) => filter.InvokeAsync(context, next);
        });
    }

    public static IEndpointConventionBuilder WithTracking(this IEndpointConventionBuilder endpoint, GlobalOptions options)
    {
        return endpoint.AddEndpointFilterFactory((provider, next) =>
        {
            var builder = provider.ApplicationServices.GetRequiredService<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
            var immutableOptions = builder.Build(options);

            var etagService = provider.ApplicationServices.GetRequiredService<IRequestHandler>();
            var requestFilter = provider.ApplicationServices.GetRequiredService<IRequestFilter>();

            var filter = new TrackerEndpointFilter(etagService, requestFilter, immutableOptions);
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

    public static IEndpointConventionBuilder WithTracking(this IEndpointConventionBuilder endpoint, Action<GlobalOptions> configure)
    {
        var options = new GlobalOptions();
        configure(options);
        return endpoint.WithTracking(options);
    }
}
