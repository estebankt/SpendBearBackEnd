using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatementImport.Domain.Entities;

namespace StatementImport.Infrastructure.Persistence.Configurations;

public class ParsedTransactionConfiguration : IEntityTypeConfiguration<ParsedTransaction>
{
    public void Configure(EntityTypeBuilder<ParsedTransaction> builder)
    {
        builder.ToTable("ParsedTransactions", "statement_import");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.StatementUploadId)
            .IsRequired();

        builder.Property(p => p.Date)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasConversion(v => (long)(v * 100), v => v / 100m)
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.SuggestedCategoryId)
            .IsRequired();

        builder.Property(p => p.OriginalText)
            .HasMaxLength(2000);

        builder.HasIndex(p => p.StatementUploadId);
    }
}
