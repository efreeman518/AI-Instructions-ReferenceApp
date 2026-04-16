using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Model;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class AttachmentConfiguration() : EntityBaseConfiguration<Attachment>(false)
{
    public override void Configure(EntityTypeBuilder<Attachment> builder)
    {
        base.Configure(builder);
        builder.ToTable("Attachment");

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.StorageUri).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.OwnerType).HasConversion<int>();

        builder.HasIndex(e => new { e.OwnerType, e.OwnerId })
            .HasDatabaseName("IX_Attachment_OwnerType_OwnerId");

        builder.HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("IX_Attachment_TenantId_Id");
    }
}
