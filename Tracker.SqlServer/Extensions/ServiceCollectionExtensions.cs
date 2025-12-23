using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.Core.Services.Contracts;
using Tracker.SqlServer.Models;
using Tracker.SqlServer.Services;

namespace Tracker.SqlServer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerProvider(
        this IServiceCollection services, string sourceId, string connectionString, TrackingMode mode = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        Func<IServiceProvider, ISourceProvider> factory = mode switch
        {
            TrackingMode.DbIndexUsageStats => (_) => new SqlServerIndexUsageOperations(sourceId, connectionString),
            TrackingMode.ChangeTracking => (_) => new SqlServerChangeTrackingOperations(sourceId, connectionString),
            _ => throw new InvalidOperationException()
        };

        return services.AddKeyedSingleton(sourceId, factory);
    }

    public static IServiceCollection AddSqlServerProvider<TContext>(this IServiceCollection services, TrackingMode mode = default)
         where TContext : DbContext
    {
        var contextFullName = typeof(TContext).FullName;

        ArgumentException.ThrowIfNullOrEmpty(contextFullName);

        return services.AddKeyedSingleton<ISourceProvider>(contextFullName, (provider, key) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            if (key is not string keyRaw)
                throw new InvalidCastException();

            return mode switch
            {
                TrackingMode.DbIndexUsageStats => new SqlServerIndexUsageOperations(keyRaw, connectionString),
                TrackingMode.ChangeTracking => new SqlServerChangeTrackingOperations(keyRaw, connectionString),
                _ => throw new InvalidOperationException()
            };
        });
    }

    public static IServiceCollection AddSqlServerProvider<TContext>(this IServiceCollection services, string providerId, TrackingMode mode = default)
         where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId, nameof(providerId));

        return services.AddKeyedSingleton<ISourceProvider>(providerId, (provider, key) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            if (key is not string keyRaw)
                throw new InvalidCastException();

            return mode switch
            {
                TrackingMode.DbIndexUsageStats => new SqlServerIndexUsageOperations(keyRaw, connectionString),
                TrackingMode.ChangeTracking => new SqlServerChangeTrackingOperations(keyRaw, connectionString),
                _ => throw new InvalidOperationException()
            };
        });
    }
}
