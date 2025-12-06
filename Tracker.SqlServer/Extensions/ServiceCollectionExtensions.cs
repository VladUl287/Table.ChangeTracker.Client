using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.Core.Extensions;
using Tracker.Core.Services.Contracts;
using Tracker.SqlServer.Services;

namespace Tracker.SqlServer.Extensions;

public static class ServiceCollectionExtensions
{
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
            var dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);

            return new SqlServerOperations(sourceId, dataSource);
        });
    }
}
