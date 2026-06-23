using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Ids;

namespace TaskFlow.Infrastructure.Data.Configurations;

/// <summary>Provides checklist item behavior for the Infrastructure Configurations layer.</summary>
public class ChecklistItemConfiguration() : EntityBaseConfiguration<ChecklistItem, ChecklistItemId>(false)
{
    /// <summary>Configures runtime behavior for this component.</summary>
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
