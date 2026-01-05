using Microsoft.EntityFrameworkCore;

namespace Tracker.Core.Services.Contracts;

public interface ITableNameResolver
{
    IEnumerable<string> GetTablesNames<TContext>(TContext context, IEnumerable<Type> entities)
        where TContext : DbContext;
}
