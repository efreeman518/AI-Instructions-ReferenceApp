using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Model;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class ChecklistItemConfiguration() : EntityBaseConfiguration<ChecklistItem>(false)
{
    public override void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        base.Configure(builder);
        builder.ToTable("ChecklistItem");

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.TaskItemId })
            .HasDatabaseName("CIX_ChecklistItem_TenantId_TaskItemId")
            .IsClustered();
    }
}
