using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SpendBear.SharedKernel;

namespace SpendBear.Infrastructure.Core.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxProcessorOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly string _connectionString;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        IOptions<OutboxProcessorOptions> options,
        ILogger<OutboxProcessor> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
        _connectionString = configuration.GetSection("ConnectionStrings")["DefaultConnection"]
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not configured");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started. Polling every {IntervalMs}ms, batch size {BatchSize}, max retries {MaxRetries}",
            _options.PollingIntervalMs, _options.BatchSize, _options.MaxRetryCount);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            try
            {
                await Task.Delay(_options.PollingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("OutboxProcessor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Fetch unprocessed messages with row-level locking
        const string selectSql = """
            SELECT id, event_type, payload, occurred_on
            FROM shared.outbox_messages
            WHERE processed_at IS NULL AND retry_count < @maxRetries
            ORDER BY created_at ASC
            LIMIT @batchSize
            FOR UPDATE SKIP LOCKED
            """;

        var messages = new List<OutboxMessage>();

        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            await using (var selectCmd = new NpgsqlCommand(selectSql, connection, transaction))
            {
                selectCmd.Parameters.AddWithValue("maxRetries", _options.MaxRetryCount);
                selectCmd.Parameters.AddWithValue("batchSize", _options.BatchSize);

                await using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        messages.Add(new OutboxMessage
                        {
                            Id = reader.GetGuid(0),
                            EventType = reader.GetString(1),
                            Payload = reader.GetString(2),
                            OccurredOn = reader.GetDateTime(3)
                        });
                    }
                } // reader disposed here
            } // command disposed here

            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var message in messages)
        {
            await ProcessSingleMessageAsync(connection, message, cancellationToken);
        }

        // Periodically clean up old processed messages
        await CleanupProcessedMessagesAsync(connection, cancellationToken);
    }

    private async Task ProcessSingleMessageAsync(NpgsqlConnection connection, OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var type = Type.GetType(message.EventType);
            if (type == null)
            {
                _logger.LogError("Could not resolve type {EventType} for outbox message {MessageId}", message.EventType, message.Id);
                await MarkFailedAsync(connection, message.Id, $"Could not resolve type: {message.EventType}", cancellationToken);
                return;
            }

            var domainEvent = JsonSerializer.Deserialize(message.Payload, type, JsonOptions) as IDomainEvent;
            if (domainEvent == null)
            {
                _logger.LogError("Could not deserialize outbox message {MessageId} as IDomainEvent", message.Id);
                await MarkFailedAsync(connection, message.Id, "Deserialization returned null", cancellationToken);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            await dispatcher.DispatchAsync(domainEvent, cancellationToken);

            // Mark as processed
            const string updateSql = "UPDATE shared.outbox_messages SET processed_at = @processedAt WHERE id = @id";
            await using var cmd = new NpgsqlCommand(updateSql, connection);
            cmd.Parameters.AddWithValue("processedAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("id", message.Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogDebug("Processed outbox message {MessageId} ({EventType})", message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process outbox message {MessageId}, will retry", message.Id);
            await MarkFailedAsync(connection, message.Id, ex.Message, cancellationToken);
        }
    }

    private static async Task MarkFailedAsync(NpgsqlConnection connection, Guid messageId, string error, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE shared.outbox_messages
            SET retry_count = retry_count + 1, last_error = @error
            WHERE id = @id
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("error", error);
        cmd.Parameters.AddWithValue("id", messageId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CleanupProcessedMessagesAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM shared.outbox_messages WHERE processed_at IS NOT NULL AND processed_at < @cutoff";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("cutoff", DateTime.UtcNow.AddDays(-_options.RetentionDays));

        var deleted = await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (deleted > 0)
        {
            _logger.LogInformation("Cleaned up {Count} processed outbox messages older than {Days} days", deleted, _options.RetentionDays);
        }
    }
}
