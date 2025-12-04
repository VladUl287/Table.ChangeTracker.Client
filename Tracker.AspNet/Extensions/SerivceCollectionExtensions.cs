using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Extensions;

public static class SerivceCollectionExtensions
{
    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        return services.AddTracker<TContext>(new GlobalOptions());
    }

    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services, GlobalOptions options)
         where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        services.AddSingleton<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>, GlobalOptionsBuilder>();

        services.AddSingleton((provider) =>
        {
            var optionsBuilder = provider.GetRequiredService<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
            return optionsBuilder.Build<TContext>(options);
        });

        services.AddSingleton<IETagGenerator, ETagGenerator>();
        services.AddSingleton<IETagService, ETagService>();

        services.AddSingleton<ISourceOperationsResolver, SourceOperationsResolver>();

        services.AddSingleton<IRequestFilter, DefaultRequestFilter>();

        return services;
    }

    public static IServiceCollection AddSqlServer(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        return services.AddSingleton<ISourceOperations>((provider) =>
            new SqlServerOperations(
                SqlClientFactory.Instance.CreateDataSource(connectionString)
            )
        );
    }

    public static IServiceCollection AddSqlServer<TContext>(this IServiceCollection services)
         where TContext : DbContext
    {
        return services.AddSingleton<ISourceOperations>((provider) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            var factory = SqlClientFactory.Instance;
            var dataSource = factory.CreateDataSource(connectionString);

            return new SqlServerOperations(dataSource);
        });
    }

    public static IServiceCollection AddNpgsql<TContext>(this IServiceCollection services)
         where TContext : DbContext
    {
        return services.AddSingleton<ISourceOperations>((provider) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            var sourceId = typeof(TContext).GetTypeHashId();
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            var dataSource = builder.Build();
            return new NpgsqlOperations(sourceId, dataSource);
        });
    }

    public static IServiceCollection AddNpgsql(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        return services.AddSingleton<ISourceOperations>(
            new NpgsqlOperations(
                string.Empty,
                new NpgsqlDataSourceBuilder(connectionString).Build()
            )
        );
    }

    public static IServiceCollection AddTracker<TContext>(this IServiceCollection services, Action<GlobalOptions> configure)
         where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new GlobalOptions();
        configure(options);
        return services.AddTracker<TContext>(options);
    }
}
