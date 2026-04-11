using EF.Data;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Model;

namespace TaskFlow.Infrastructure.Data;

public class TaskFlowDbContextTrxn : DbContextBase<string, Guid?>
{
    public TaskFlowDbContextTrxn(DbContextOptions options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<TaskItemTag> TaskItemTags => Set<TaskItemTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("taskflow");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskFlowDbContextTrxn).Assembly);
    }
}
