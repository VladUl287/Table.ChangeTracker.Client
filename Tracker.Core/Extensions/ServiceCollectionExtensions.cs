using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Tracker.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNpgsqlDataSource<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        return services.AddSingleton((provider) =>
        {
            var dbContext = provider.GetRequiredService<TContext>();

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(dbContext.Database.GetConnectionString());

            dataSourceBuilder.ConnectionStringBuilder.Pooling = true;
            dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = 100;
            dataSourceBuilder.ConnectionStringBuilder.MinPoolSize = 0;

            return dataSourceBuilder.Build();
        });
    }
}
