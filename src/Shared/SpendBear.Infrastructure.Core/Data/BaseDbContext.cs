using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SpendBear.SharedKernel;

namespace SpendBear.Infrastructure.Core.Data;

/// <summary>
/// Base DbContext for all module contexts.
/// Writes domain events to the outbox table atomically with business data.
/// </summary>
public abstract class BaseDbContext : DbContext, IUnitOfWork
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Saves changes and writes domain events to the outbox table atomically.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events before saving
        var aggregatesWithEvents = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        // Clear events from aggregates before save
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        // Save business data
        var result = await base.SaveChangesAsync(cancellationToken);

        // Write outbox messages using the same connection/transaction
        if (domainEvents.Count > 0)
        {
            var connection = Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            var transaction = Database.CurrentTransaction?.GetDbTransaction();

            foreach (var domainEvent in domainEvents)
            {
                var eventType = domainEvent.GetType();
                var assemblyQualifiedName = $"{eventType.FullName}, {eventType.Assembly.GetName().Name}";
                var payload = JsonSerializer.Serialize(domainEvent, eventType, JsonOptions);

                const string sql = """
                    INSERT INTO shared.outbox_messages (id, event_type, payload, occurred_on, created_at, source_module)
                    VALUES (@id, @eventType, @payload::jsonb, @occurredOn, @createdAt, @sourceModule)
                    """;

                await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection, (NpgsqlTransaction?)transaction);
                command.Parameters.AddWithValue("id", domainEvent.EventId);
                command.Parameters.AddWithValue("eventType", assemblyQualifiedName);
                command.Parameters.AddWithValue("payload", payload);
                command.Parameters.AddWithValue("occurredOn", DateTime.SpecifyKind(domainEvent.OccurredOn, DateTimeKind.Utc));
                command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("sourceModule", GetType().Name.Replace("DbContext", ""));

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.RollbackTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore domain events collection
        modelBuilder.Ignore<IDomainEvent>();

        // Global DateTime converter for PostgreSQL UTC requirements
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

            foreach (var property in properties)
            {
                property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                    v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                    v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc)));
            }
        }
    }
}
