using Microsoft.Data.SqlClient;

namespace Tracker.SqlServer.Tests.Utils;

internal static class SqlServerHelpers
{
    internal static async Task DisableChangeTrackingForAllTables(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Get all tables with change tracking enabled
        var getTrackedTablesQuery = @"
        SELECT 
            SCHEMA_NAME(t.schema_id) AS SchemaName,
            t.name AS TableName
        FROM sys.change_tracking_tables ct
        INNER JOIN sys.tables t ON ct.object_id = t.object_id
        WHERE ct.is_track_columns_updated_on = 1";

        var tablesToDisable = new List<(string Schema, string Table)>();

        using (var command = new SqlCommand(getTrackedTablesQuery, connection))
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                string schema = reader.GetString(0);
                string table = reader.GetString(1);
                tablesToDisable.Add((schema, table));
            }
        }

        // Disable change tracking for each table
        foreach (var (schema, table) in tablesToDisable)
        {
            var disableTableQuery = $@"
            ALTER TABLE [{schema}].[{table}] 
            DISABLE CHANGE_TRACKING";

            try
            {
                using var disableCommand = new SqlCommand(disableTableQuery, connection);
                await disableCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"Disabled change tracking for table: {schema}.{table}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disabling change tracking for table {schema}.{table}: {ex.Message}");
                // Continue with other tables even if one fails
            }
        }
    }

    internal static async Task DisableDatabaseChangeTracking(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // First, check if change tracking is enabled for the database
        var checkQuery = @"
        SELECT 
            CASE 
                WHEN database_id IS NOT NULL THEN 1 
                ELSE 0 
            END AS IsTrackingEnabled
        FROM sys.change_tracking_databases 
        WHERE database_id = DB_ID()";

        bool isTrackingEnabled = false;

        using (var checkCommand = new SqlCommand(checkQuery, connection))
        using (var reader = await checkCommand.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                isTrackingEnabled = reader.GetInt32(0) == 1;
            }
        }

        // If change tracking is enabled, disable it
        if (isTrackingEnabled)
        {
            // First disable change tracking for all tables (optional but recommended)
            await DisableChangeTrackingForAllTables(connectionString);

            // Then disable change tracking for the database
            var disableQuery = "ALTER DATABASE CURRENT SET CHANGE_TRACKING = OFF";

            try
            {
                using var disableCommand = new SqlCommand(disableQuery, connection);
                await disableCommand.ExecuteNonQueryAsync();
                Console.WriteLine("Database change tracking has been disabled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disabling database change tracking: {ex.Message}");
                throw;
            }
        }
        else
        {
            Console.WriteLine("Change tracking is already disabled for this database.");
        }
    }

    internal static async Task EnableDatabaseChangeTracking(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var checkQuery = "SELECT is_auto_cleanup_on, retention_period, retention_period_units FROM sys.change_tracking_databases WHERE database_id = DB_ID()";
        using var checkCommand = new SqlCommand(checkQuery, connection);
        using var reader = await checkCommand.ExecuteReaderAsync();

        if (!reader.HasRows)
        {
            await reader.CloseAsync();
            var enableQuery = "ALTER DATABASE CURRENT SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON)";
            using var enableCommand = new SqlCommand(enableQuery, connection);
            await enableCommand.ExecuteNonQueryAsync();
        }
        else
        {
            await reader.CloseAsync();
        }
    }

    internal static async Task CreateTestTable(string connectionString, string tableName)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var createTable = $@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{tableName}')
                BEGIN
                    CREATE TABLE [{tableName}] (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Value INT NOT NULL
                    )
                END";

        using var command = new SqlCommand(createTable, connection);
        await command.ExecuteNonQueryAsync();
    }

    internal static async Task CreateTestTableWithNoKey(string connectionString, string tableName)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var createQuery = $@"
            CREATE TABLE [{tableName}] (
                Id INT NOT NULL,
                Name NVARCHAR(100) NOT NULL
            )";

        using var command = new SqlCommand(createQuery, connection);
        await command.ExecuteNonQueryAsync();
    }

    internal static async Task InsertToTestTable(string connectionString, string table, int value)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var insertQuery = $"INSERT INTO [{table}] (Value) VALUES (1)";
        using var insertCommand = new SqlCommand(insertQuery, connection);
        await insertCommand.ExecuteNonQueryAsync();
    }

    internal static async Task DropTable(string connectionString, string tableName)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var disableTracking1 = $"ALTER TABLE [{tableName}] DISABLE CHANGE_TRACKING";

        try
        {
            using var cmd1 = new SqlCommand(disableTracking1, connection);
            await cmd1.ExecuteNonQueryAsync();
        }
        catch { /* Ignore if not enabled */ }

        var dropTable1 = $"DROP TABLE IF EXISTS [{tableName}]";

        using var cmd3 = new SqlCommand(dropTable1, connection);
        await cmd3.ExecuteNonQueryAsync();
    }

    internal static async Task MakeDbChanges(string connectionString)
    {
        var tempTableName = $"TempTable_{Guid.NewGuid():N}";

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var createTable = $"CREATE TABLE [{tempTableName}] (Id INT PRIMARY KEY)";
        using var createCommand = new SqlCommand(createTable, connection);
        await createCommand.ExecuteNonQueryAsync();

        var enableTracking = $"ALTER TABLE [{tempTableName}] ENABLE CHANGE_TRACKING";
        using var enableCommand = new SqlCommand(enableTracking, connection);
        await enableCommand.ExecuteNonQueryAsync();

        for (int i = 0; i < 3; i++)
        {
            var insertQuery = $"INSERT INTO [{tempTableName}] (Id) VALUES ({i})";
            using var insertCommand = new SqlCommand(insertQuery, connection);
            await insertCommand.ExecuteNonQueryAsync();
        }

        var disableTracking = $"ALTER TABLE [{tempTableName}] DISABLE CHANGE_TRACKING";
        using var disableCommand = new SqlCommand(disableTracking, connection);
        await disableCommand.ExecuteNonQueryAsync();

        var dropTable = $"DROP TABLE [{tempTableName}]";
        using var dropCommand = new SqlCommand(dropTable, connection);
        await dropCommand.ExecuteNonQueryAsync();
    }
}
