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
        var contextFullName = typeof(TContext).FullName;
        ArgumentException.ThrowIfNullOrEmpty(contextFullName);
        return services.AddNpgsqlProvider<TContext>(contextFullName);
    }

    public static IServiceCollection AddNpgsqlProvider<TContext>(this IServiceCollection services, string providerId)
         where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId);

        ISourceProvider factory(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            return new NpgsqlOperations(providerId, connectionString);
        }

        return services
            .AddSingleton(factory)
            .AddKeyedSingleton(providerId, (provider, _) => factory(provider));
    }

    public static IServiceCollection AddNpgsqlProvider(this IServiceCollection services, string providerId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        return services
            .AddSingleton<ISourceProvider>((_) => new NpgsqlOperations(providerId, connectionString))
            .AddKeyedSingleton<ISourceProvider>(providerId, (_, _) => new NpgsqlOperations(providerId, connectionString));
    }

    public static IServiceCollection AddNpgsqlSource(this IServiceCollection services, string providerId, Action<NpgsqlDataSourceBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentException.ThrowIfNullOrEmpty(providerId);

        ISourceProvider factory(Action<NpgsqlDataSourceBuilder> configure)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder();
            configure(dataSourceBuilder);
            var dataSource = dataSourceBuilder.Build();

            return new NpgsqlOperations(providerId, dataSource);
        }

        return services
            .AddSingleton((_) => factory(configure))
            .AddKeyedSingleton(providerId, (_, _) => factory(configure));
    }
}
