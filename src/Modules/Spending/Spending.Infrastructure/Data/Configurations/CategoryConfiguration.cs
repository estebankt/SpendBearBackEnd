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

        // Index for user queries
        builder.HasIndex(c => c.UserId);

        // Unique constraint: user can't have duplicate category names
        builder.HasIndex(c => new { c.UserId, c.Name })
            .IsUnique();
    }
}
