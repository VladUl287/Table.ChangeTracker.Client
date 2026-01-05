using Microsoft.EntityFrameworkCore;
using Tracker.Core.Services.Contracts;

namespace Tracker.Core.Services;

public sealed class DefaultTableNameResolver : ITableNameResolver
{
    public IEnumerable<string> GetTablesNames<TContext>(TContext context, IEnumerable<Type> entities)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        foreach (var entity in entities)
        {
            var entityType = context.Model.FindEntityType(entity) ??
                throw new NullReferenceException($"Table entity type not found for type '{entity.FullName}'");

            var tableName = entityType.GetSchemaQualifiedTableName() ??
                throw new NullReferenceException($"Table name not found for type '{entity.FullName}'");

            yield return tableName;
        }
    }
}
