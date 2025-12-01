using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Tracker.Core.Extensions;

public static class DbContextExtensions
{
    public static string[] GetTablesNames<TContext>(this TContext context, Type[] entities) where TContext : DbContext
    {
        var result = new HashSet<string>();

        foreach (var entity in entities ?? [])
        {
            var entityType = context.Model.FindEntityType(entity) ??
                throw new NullReferenceException($"Table entity type not found for type {entity.FullName}");

            var tableName = entityType.GetSchemaQualifiedTableName() ??
                throw new NullReferenceException($"Table entity type not found for type {entity.FullName}");

            result.Add(tableName);
        }

        return [.. result];
    }

    public static async Task<bool> EnableTableTracking(this DbContext dbContext, string tableName, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(dbContext, nameof(dbContext));
        ArgumentException.ThrowIfNullOrEmpty(tableName, nameof(tableName));

        var database = dbContext.Database;
        var connection = database.GetDbConnection();

        await using var command = connection.CreateCommand();

        await connection.OpenAsync(token);
        try
        {
            command.CommandText = "SELECT enable_table_tracking(@table_name);";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@table_name";
            parameter.Value = tableName;
            parameter.DbType = DbType.String;
            command.Parameters.Add(parameter);

            var result = (bool?)await command.ExecuteScalarAsync(token);
            return result ?? false;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public static async Task<bool> DisableTableTracking(this DbContext dbContext, string tableName, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(dbContext, nameof(dbContext));
        ArgumentException.ThrowIfNullOrEmpty(tableName, nameof(tableName));

        var database = dbContext.Database;
        var connection = database.GetDbConnection();

        await using var command = connection.CreateCommand();

        await connection.OpenAsync(token);
        try
        {
            command.CommandText = "SELECT disable_table_tracking(@table_name);";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@table_name";
            parameter.Value = tableName;
            parameter.DbType = DbType.String;
            command.Parameters.Add(parameter);

            var result = (bool?)await command.ExecuteScalarAsync(token);
            return result ?? false;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public static async Task<bool> IsTableTracked(this DbContext dbContext, string tableName, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(dbContext, nameof(dbContext));
        ArgumentException.ThrowIfNullOrEmpty(tableName, nameof(tableName));

        var database = dbContext.Database;
        var connection = database.GetDbConnection();

        await using var command = connection.CreateCommand();

        await connection.OpenAsync(token);
        try
        {
            command.CommandText = "SELECT is_table_tracked(@table_name);";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@table_name";
            parameter.Value = tableName;
            parameter.DbType = DbType.String;
            command.Parameters.Add(parameter);

            var result = (bool?)await command.ExecuteScalarAsync(token);
            return result ?? false;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public static async Task<string?> GetLastTimestamp(this DbContext dbContext, string tableName, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(dbContext, nameof(dbContext));
        ArgumentException.ThrowIfNullOrEmpty(tableName, nameof(tableName));

        var database = dbContext.Database;
        var connection = database.GetDbConnection();

        await using var command = connection.CreateCommand();

        await connection.OpenAsync(token);

        try
        {
            command.CommandText = "SELECT get_last_timestamp(@table_name);";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@table_name";
            parameter.Value = tableName;
            parameter.DbType = DbType.String;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(token);
            return result?.ToString();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public static async Task<uint?> GetLastCommittedXact(this DbContext dbContext, CancellationToken token)
    {
        var database = dbContext.Database;
        var connection = database.GetDbConnection();
        var transaction = database.CurrentTransaction?.GetDbTransaction();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        await connection.OpenAsync(token);

        try
        {
            command.CommandText = "SELECT pg_last_committed_xact();";

            var results = (object?[]?)await command.ExecuteScalarAsync(token);
            var xid = results?[0];
            return xid is null ? null : (uint)xid;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
