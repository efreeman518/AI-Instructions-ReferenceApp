using EF.Data;
using EF.Domain.Contracts;
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
        ConfigureDefaultDataTypes(modelBuilder);
        ConfigureTenantQueryFilters(modelBuilder);
    }

    private static void ConfigureDefaultDataTypes(ModelBuilder modelBuilder)
    {
        var decimalProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
            .Where(p => p.GetColumnType() == null);
        foreach (var property in decimalProperties)
            property.SetColumnType("decimal(10,4)");

        var dateProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
            .Where(p => p.GetColumnType() == null);
        foreach (var property in dateProperties)
            property.SetColumnType("datetime2");
    }

    private void ConfigureTenantQueryFilters(ModelBuilder modelBuilder)
    {
        var tenantEntityClrTypes = modelBuilder.Model.GetEntityTypes()
            .Where(et => typeof(ITenantEntity<Guid>).IsAssignableFrom(et.ClrType))
            .Select(et => et.ClrType);

        foreach (var clrType in tenantEntityClrTypes)
        {
            var filter = BuildTenantFilter(clrType);
            modelBuilder.Entity(clrType).HasQueryFilter(filter);
        }
    }
}
