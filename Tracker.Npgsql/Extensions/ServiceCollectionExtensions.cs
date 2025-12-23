using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tracker.Core.Services.Contracts;
using Tracker.Npgsql.Services;

namespace Tracker.Npgsql.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNpgsqlProvider<TContext>(this IServiceCollection services)
         where TContext : DbContext
    {
        var fullName = typeof(TContext).FullName;
        ArgumentException.ThrowIfNullOrEmpty(fullName);
        return services.AddNpgsqlProvider<TContext>(fullName);
    }

    public static IServiceCollection AddNpgsqlProvider<TContext>(this IServiceCollection services, string providerId)
         where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId);

        return services.AddKeyedSingleton<ISourceProvider>(providerId, (provider, key) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            if (key is not string keyRaw)
                throw new InvalidCastException();

            return new NpgsqlOperations(keyRaw, connectionString);
        });
    }

    public static IServiceCollection AddNpgsqlProvider(this IServiceCollection services, string providerId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        return services.AddKeyedSingleton<ISourceProvider>(providerId, (_, key) =>
        {
            if (key is not string keyRaw)
                throw new InvalidCastException();

            return new NpgsqlOperations(keyRaw, connectionString);
        });
    }

    public static IServiceCollection AddNpgsqlSource(this IServiceCollection services, string providerId, Action<NpgsqlDataSourceBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentException.ThrowIfNullOrEmpty(providerId);

        return services.AddKeyedSingleton<ISourceProvider>(providerId, (_, key) =>
        {
            if (key is not string keyRaw)
                throw new InvalidCastException();

            var dataSourceBuilder = new NpgsqlDataSourceBuilder();
            configure(dataSourceBuilder);
            var dataSource = dataSourceBuilder.Build();

            return new NpgsqlOperations(keyRaw, dataSource);
        });
    }
}
