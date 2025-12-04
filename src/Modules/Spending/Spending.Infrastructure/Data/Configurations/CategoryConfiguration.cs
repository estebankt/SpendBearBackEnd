using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spending.Domain.Entities;

namespace Spending.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.IsSystemCategory)
            .IsRequired()
            .HasDefaultValue(false);

        // Index for user queries
        builder.HasIndex(c => c.UserId);

        // Note: Partial unique indexes are created in migration via raw SQL
        // EF Core doesn't support partial indexes declaratively
        // - User categories: unique per (UserId, Name) where IsSystemCategory = false
        // - System categories: unique globally by Name where IsSystemCategory = true
    }
}
