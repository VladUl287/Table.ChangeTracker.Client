using Npgsql;

namespace Tracker.Npgsql.Tests.Utils;

internal static class SqlHelpers
{
    internal static async Task CreateTestTable(string connectionString, string tableName)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        using var createTableCmd = new NpgsqlCommand(
            $@"CREATE TABLE IF NOT EXISTS {tableName} (
                id SERIAL PRIMARY KEY,
                value INT
            )", connection);

        await createTableCmd.ExecuteNonQueryAsync();
    }

    internal static async Task InsertToTestTable(string connectionString, string tableName, int value)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        using var createTableCmd = new NpgsqlCommand(
            $@"
                INSERT INTO {tableName}(
	            ""value"")
	            VALUES ({value});
            ", connection);

        await createTableCmd.ExecuteNonQueryAsync();
    }

    internal static async Task InsertToTestFromTestTable(string connectionString, string sourceTableName, string destTableName)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        using var createTableCmd = new NpgsqlCommand(
            $@"
                INSERT INTO {destTableName}(""value"")
                SELECT ""value"" FROM {sourceTableName};
            ", connection);

        await createTableCmd.ExecuteNonQueryAsync();
    }

    public static async Task<long> GetDatabaseTimestampAsync(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        const string query = "SELECT CURRENT_TIMESTAMP;";
        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var timestamp = reader.GetFieldValue<DateTimeOffset>(0).UtcTicks;
        return timestamp;
    }

    internal static async Task DropTable(string connectionString, string tableName)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        using var createTableCmd = new NpgsqlCommand($"DROP TABLE IF EXISTS {tableName}", connection);

        await createTableCmd.ExecuteNonQueryAsync();
    }

    internal static async Task CreateDatabase(string connectionString, string databaseName)
    {
        try { await DropDatabase(connectionString, databaseName); }
        catch { }

        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        using var createDatabaseCommand = new NpgsqlCommand($"CREATE DATABASE {databaseName}", connection);
        await createDatabaseCommand.ExecuteNonQueryAsync();
    }

    internal static async Task DropDatabase(string connectionString, string databaseName)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        using var dropDatabaseCommand = new NpgsqlCommand($"DROP DATABASE IF EXISTS {databaseName}", connection);
        await dropDatabaseCommand.ExecuteNonQueryAsync();
    }
}
