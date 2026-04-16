using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Model;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class CommentConfiguration() : EntityBaseConfiguration<Comment>(false)
{
    public override void Configure(EntityTypeBuilder<Comment> builder)
    {
        base.Configure(builder);
        builder.ToTable("Comment");

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(2000).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.TaskItemId })
            .HasDatabaseName("CIX_Comment_TenantId_TaskItemId")
            .IsClustered();
    }
}
