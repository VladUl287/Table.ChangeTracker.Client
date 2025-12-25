using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.Core.Services.Contracts;
using Tracker.SqlServer.Models;
using Tracker.SqlServer.Services;

namespace Tracker.SqlServer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerProvider(
        this IServiceCollection services, string providerId, string connectionString, TrackingMode mode = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        Func<IServiceProvider, ISourceProvider> factory = mode switch
        {
            TrackingMode.DbIndexUsageStats => (_) => new SqlServerIndexUsageOperations(providerId, connectionString),
            TrackingMode.ChangeTracking => (_) => new SqlServerChangeTrackingOperations(providerId, connectionString),
            _ => throw new InvalidOperationException()
        };

        return services
            .AddSingleton(factory)
            .AddKeyedSingleton(providerId, factory);
    }

    public static IServiceCollection AddSqlServerProvider<TContext>(this IServiceCollection services, TrackingMode mode = default)
         where TContext : DbContext
    {
        var contextFullName = typeof(TContext).FullName;
        ArgumentException.ThrowIfNullOrEmpty(contextFullName);
        return services.AddSqlServerProvider<TContext>(contextFullName, mode);
    }

    public static IServiceCollection AddSqlServerProvider<TContext>(this IServiceCollection services, string providerId, TrackingMode mode = default)
         where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId, nameof(providerId));

        ISourceProvider factory(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            return mode switch
            {
                TrackingMode.DbIndexUsageStats => new SqlServerIndexUsageOperations(providerId, connectionString),
                TrackingMode.ChangeTracking => new SqlServerChangeTrackingOperations(providerId, connectionString),
                _ => throw new InvalidOperationException()
            };
        }

        return services
            .AddSingleton(factory)
            .AddKeyedSingleton(providerId, (provider, _) => factory(provider));
    }
}
