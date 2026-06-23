using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Ids;

namespace TaskFlow.Infrastructure.Data.Configurations;

/// <summary>Provides task item tag behavior for the Infrastructure Configurations layer.</summary>
public class TaskItemTagConfiguration() : EntityBaseConfiguration<TaskItemTag, TaskItemTagId>(false)
{
    /// <summary>Configures runtime behavior for this component.</summary>
    public override void Configure(EntityTypeBuilder<TaskItemTag> builder)
    {
        base.Configure(builder);
        builder.ToTable("TaskItemTag");

        builder.Property(e => e.TenantId).IsRequired();

        builder.HasIndex(e => new { e.TaskItemId, e.TagId })
            .HasDatabaseName("IX_TaskItemTag_TaskItemId_TagId")
            .IsUnique();

        builder.HasOne(e => e.TaskItem)
            .WithMany(e => e.TaskItemTags)
            .HasForeignKey(e => e.TaskItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tag)
            .WithMany(e => e.TaskItemTags)
            .HasForeignKey(e => e.TagId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.TaskItemId })
            .HasDatabaseName("CIX_TaskItemTag_TenantId_TaskItemId")
            .IsClustered();
    }
}
