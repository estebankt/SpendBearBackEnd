using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatementImport.Domain.Entities;

namespace StatementImport.Infrastructure.Persistence.Configurations;

public class StatementUploadConfiguration : IEntityTypeConfiguration<StatementUpload>
{
    public void Configure(EntityTypeBuilder<StatementUpload> builder)
    {
        builder.ToTable("StatementUploads", "statement_import");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.OriginalFileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.StoredFilePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(s => s.UploadedAt)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired();

        builder.Property(s => s.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasMany(s => s.ParsedTransactions)
            .WithOne()
            .HasForeignKey(p => p.StatementUploadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId);

        builder.Metadata.FindNavigation(nameof(StatementUpload.ParsedTransactions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
