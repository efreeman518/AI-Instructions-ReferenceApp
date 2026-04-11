using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Model;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class TaskItemTagConfiguration : IEntityTypeConfiguration<TaskItemTag>
{
    public void Configure(EntityTypeBuilder<TaskItemTag> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasIndex(e => new { e.TaskItemId, e.TagId }).IsUnique();

        builder.HasOne(e => e.TaskItem)
            .WithMany(e => e.TaskItemTags)
            .HasForeignKey(e => e.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tag)
            .WithMany(e => e.TaskItemTags)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
