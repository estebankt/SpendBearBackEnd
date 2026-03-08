using System.Reflection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SpendBear.Infrastructure.Core.Outbox;

public static class OutboxTableInitializer
{
    private static readonly string Sql = LoadSql();

    private static string LoadSql()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "SpendBear.Infrastructure.Core.Outbox.outbox-init.sql";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static async Task EnsureOutboxTableAsync(string connectionString, ILogger logger)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(Sql, connection);
        await command.ExecuteNonQueryAsync();

        logger.LogInformation("Outbox table ensured in shared schema");
    }
}
