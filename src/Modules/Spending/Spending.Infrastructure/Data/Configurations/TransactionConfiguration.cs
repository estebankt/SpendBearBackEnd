using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spending.Domain.Entities;

namespace Spending.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions", "spending");

        builder.HasKey(t => t.Id);

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasConversion(v => (long)(v * 100), v => v / 100m);
            
            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.UserId)
            .IsRequired();
            
        builder.Property(t => t.CategoryId)
            .IsRequired();
    }
}
