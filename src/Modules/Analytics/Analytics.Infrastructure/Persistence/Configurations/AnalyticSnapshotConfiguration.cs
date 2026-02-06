using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;

namespace Analytics.Infrastructure.Persistence.Configurations;

public class AnalyticSnapshotConfiguration : IEntityTypeConfiguration<AnalyticSnapshot>
{
    public void Configure(EntityTypeBuilder<AnalyticSnapshot> builder)
    {
        builder.ToTable("analytic_snapshots", "analytics");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.SnapshotDate)
            .IsRequired();

        builder.Property(s => s.Period)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(s => s.TotalIncome)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.TotalExpense)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.NetBalance)
            .HasColumnType("decimal(18,2)");

        var dictionaryComparer = new ValueComparer<Dictionary<Guid, decimal>>(
            (a, b) => a != null && b != null && a.Count == b.Count && !a.Except(b).Any(),
            v => v.Aggregate(0, (hash, kvp) => HashCode.Combine(hash, kvp.Key, kvp.Value)),
            v => new Dictionary<Guid, decimal>(v));

        builder.Property(s => s.SpendingByCategory)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<Guid, decimal>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                     ?? new Dictionary<Guid, decimal>())
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(s => s.IncomeByCategory)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<Guid, decimal>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                     ?? new Dictionary<Guid, decimal>())
            .Metadata.SetValueComparer(dictionaryComparer);

        // Ensure unique constraint for UserId, SnapshotDate, and Period to prevent duplicate snapshots
        builder.HasIndex(s => new { s.UserId, s.SnapshotDate, s.Period })
            .IsUnique();
    }
}
