using Budgets.Domain.Entities;
using Budgets.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budgets.Infrastructure.Persistence.Configurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(b => b.Period)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(b => b.StartDate)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(b => b.EndDate)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(b => b.UserId)
            .IsRequired();

        builder.Property(b => b.CategoryId)
            .IsRequired(false);

        builder.Property(b => b.CurrentSpent)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.WarningThreshold)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(b => b.IsExceeded)
            .IsRequired();

        builder.Property(b => b.WarningTriggered)
            .IsRequired();

        // Indexes for query performance
        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => new { b.UserId, b.StartDate, b.EndDate });
        builder.HasIndex(b => new { b.UserId, b.CategoryId });

        // Ignore domain events and computed properties
        builder.Ignore(b => b.DomainEvents);
        builder.Ignore(b => b.RemainingAmount);
        builder.Ignore(b => b.PercentageUsed);
    }
}
