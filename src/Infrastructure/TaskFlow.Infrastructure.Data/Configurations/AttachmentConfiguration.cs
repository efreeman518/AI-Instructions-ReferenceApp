using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Model;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.StorageUri).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.OwnerType).HasConversion<int>();
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasIndex(e => new { e.OwnerType, e.OwnerId });
    }
}
