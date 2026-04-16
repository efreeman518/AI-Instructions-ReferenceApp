using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Model;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class TaskItemConfiguration() : EntityBaseConfiguration<TaskItem>(false)
{
    public override void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        base.Configure(builder);
        builder.ToTable("TaskItem");

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Priority).HasConversion<int>();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.Features).HasConversion<int>();
        builder.Property(e => e.EstimatedEffort).HasPrecision(10, 2);
        builder.Property(e => e.ActualEffort).HasPrecision(10, 2);

        builder.OwnsOne(e => e.DateRange, dr =>
        {
            dr.Property(d => d.StartDate).HasColumnName("StartDate");
            dr.Property(d => d.DueDate).HasColumnName("DueDate");
        });

        builder.OwnsOne(e => e.RecurrencePattern, rp =>
        {
            rp.Property(r => r.Interval).HasColumnName("RecurrenceInterval");
            rp.Property(r => r.Frequency).HasColumnName("RecurrenceFrequency").HasMaxLength(50);
            rp.Property(r => r.EndDate).HasColumnName("RecurrenceEndDate");
        });

        builder.HasOne(e => e.ParentTaskItem)
            .WithMany(e => e.SubTasks)
            .HasForeignKey(e => e.ParentTaskItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Comments)
            .WithOne(e => e.TaskItem)
            .HasForeignKey(e => e.TaskItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ChecklistItems)
            .WithOne(e => e.TaskItem)
            .HasForeignKey(e => e.TaskItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Clustered composite index for tenant-scoped queries
        builder.HasIndex(e => new { e.TenantId, e.Id })
            .HasDatabaseName("CIX_TaskItem_TenantId_Id")
            .IsUnique()
            .IsClustered();

        builder.HasIndex(e => e.Status).HasDatabaseName("IX_TaskItem_Status");
        builder.HasIndex(e => e.Priority).HasDatabaseName("IX_TaskItem_Priority");
        builder.HasIndex(e => e.CategoryId).HasDatabaseName("IX_TaskItem_CategoryId");
    }
}
