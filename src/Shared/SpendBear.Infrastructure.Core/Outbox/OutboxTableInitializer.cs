using Microsoft.Extensions.Logging;
using Npgsql;

namespace SpendBear.Infrastructure.Core.Outbox;

public static class OutboxTableInitializer
{
    public static async Task EnsureOutboxTableAsync(string connectionString, ILogger logger)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS shared;

            CREATE TABLE IF NOT EXISTS shared.outbox_messages (
                id              UUID PRIMARY KEY,
                event_type      TEXT NOT NULL,
                payload         JSONB NOT NULL,
                occurred_on     TIMESTAMPTZ NOT NULL,
                created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                processed_at    TIMESTAMPTZ NULL,
                retry_count     INTEGER NOT NULL DEFAULT 0,
                last_error      TEXT NULL,
                source_module   TEXT NOT NULL DEFAULT ''
            );

            CREATE INDEX IF NOT EXISTS ix_outbox_unprocessed
                ON shared.outbox_messages (created_at ASC)
                WHERE processed_at IS NULL;

            CREATE INDEX IF NOT EXISTS ix_outbox_processed_cleanup
                ON shared.outbox_messages (processed_at)
                WHERE processed_at IS NOT NULL;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();

        logger.LogInformation("Outbox table ensured in shared schema");
    }
}
