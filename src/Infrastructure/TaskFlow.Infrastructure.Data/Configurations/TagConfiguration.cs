using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Model;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class TagConfiguration() : EntityBaseConfiguration<Tag>(true)
{
    public override void Configure(EntityTypeBuilder<Tag> builder)
    {
        base.Configure(builder);
        builder.ToTable("Tag");

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Color).HasMaxLength(7);

        builder.HasIndex(e => new { e.TenantId, e.Name })
            .HasDatabaseName("IX_Tag_TenantId_Name")
            .IsUnique();

        builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_Tag_TenantId");
    }
}
