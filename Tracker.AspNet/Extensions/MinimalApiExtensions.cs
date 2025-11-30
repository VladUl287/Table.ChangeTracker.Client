using Microsoft.AspNetCore.Builder;

namespace Tracker.AspNet.Extensions;

public static class MinimalApiExtensions
{
    public static IEndpointConventionBuilder WithTracking(
        this IEndpointConventionBuilder endpoint, string? route = null, string[]? tables = null, Type[]? entities = null)
    {
        endpoint.WithMetadata(new TrackRouteMetadata(route, tables, entities));
        return endpoint;
    }

    public static IEndpointConventionBuilder WithTracking(this IEndpointConventionBuilder endpoint, TrackRouteMetadata metadata)
    {
        endpoint.WithMetadata(metadata);
        return endpoint;
    }
}

public sealed record TrackRouteMetadata(string? Route = null, string[]? Tables = null, Type[]? Entities = null);
