using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Reflection;
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

        services.AddSingleton<IETagGenerator>(new ETagGenerator(Assembly.GetExecutingAssembly()));
        services.AddSingleton<IETagService, ETagService>();

        services.AddSingleton<ISourceOperationsResolver, SourceOperationsResolver>();

        services.AddSingleton<IRequestFilter, DefaultRequestFilter>();

        services.AddSingleton<IStartupFilter, SourceOperationsValidator>();

        return services;
    }

    public static IServiceCollection AddSqlServerSource(this IServiceCollection services, string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        return services.AddSingleton<ISourceOperations>((provider) =>
            new SqlServerOperations(
               sourceId,
               SqlClientFactory.Instance.CreateDataSource(connectionString)
            )
        );
    }

    public static IServiceCollection AddSqlServerSource<TContext>(this IServiceCollection services)
         where TContext : DbContext
    {
        return services.AddSingleton<ISourceOperations>((provider) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            var sourceId = typeof(TContext).GetTypeHashId();
            var factory = SqlClientFactory.Instance;
            var dataSource = factory.CreateDataSource(connectionString);

            return new SqlServerOperations(sourceId, dataSource);
        });
    }

    public static IServiceCollection AddNpgsqlSource<TContext>(this IServiceCollection services)
         where TContext : DbContext
    {
        var sourceId = typeof(TContext).GetTypeHashId();
        return services.AddNpgsqlSource<TContext>(sourceId);
    }

    public static IServiceCollection AddNpgsqlSource<TContext>(this IServiceCollection services, string sourceId)
         where TContext : DbContext
    {
        return services.AddSingleton<ISourceOperations>((provider) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            var builder = new NpgsqlDataSourceBuilder(connectionString);
            var dataSource = builder.Build();

            return new NpgsqlOperations(sourceId, dataSource);
        });
    }

    public static IServiceCollection AddNpgsqlSource(this IServiceCollection services, string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        return services.AddSingleton<ISourceOperations>(
            new NpgsqlOperations(
                sourceId, new NpgsqlDataSourceBuilder(connectionString).Build()
            )
        );
    }

    public static IServiceCollection AddNpgsqlSource(this IServiceCollection services, string sourceId, Action<NpgsqlDataSourceBuilder> configure)
    {
        return services.AddSingleton<ISourceOperations>((_) =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder();
            configure(dataSourceBuilder);
            var dataSource = dataSourceBuilder.Build();
            return new NpgsqlOperations(sourceId, dataSource);
        });
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
