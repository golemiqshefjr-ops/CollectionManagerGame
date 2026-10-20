using Npgsql;

namespace CollectionManager.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(PostgresConnectionFactory factory)
    {
        await using var connection = factory.Create();
        await connection.OpenAsync();

        foreach (var fileName in new[] { "01_CreateDatabase.sql", "02_ProceduresAndViews.sql" })
        {
            var path = Path.Combine(AppContext.BaseDirectory, "SQL", fileName);
            var script = await File.ReadAllTextAsync(path);
            foreach (var statement in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.IsNullOrWhiteSpace(statement)) continue;
                await using var command = new NpgsqlCommand(statement, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
