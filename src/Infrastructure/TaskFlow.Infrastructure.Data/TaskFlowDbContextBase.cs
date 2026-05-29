using EF.Data;
using EF.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Model;

namespace TaskFlow.Infrastructure.Data;

/// <summary>
/// Shared EF model for read and write DbContexts. Centralizes schema, table naming,
/// default SQL types, entity configurations, and tenant query filters.
/// </summary>
public abstract class TaskFlowDbContextBase(DbContextOptions options) : DbContextBase<string, Guid?>(options)
{
    /// <summary>
    /// Builds the TaskFlow model once for derived contexts. Derived contexts only choose tracking
    /// and connection behavior; entity mapping stays identical.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("taskflow");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskFlowDbContextBase).Assembly);
        ConfigureDefaultDataTypes(modelBuilder);
        SetTableNames(modelBuilder);
        ConfigureTenantQueryFilters(modelBuilder);
    }

    /// <summary>Provides the set table names operation for task flow DB context base.</summary>
    private static void SetTableNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Do not force a table name for owned types; they share the owner's table
            if (entity.IsOwned()) continue;

            var current = entity.GetTableName();
            if (string.IsNullOrWhiteSpace(current))
            {
                entity.SetTableName(entity.DisplayName());
            }
        }
    }

    /// <summary>Configures default data types behavior for this component.</summary>
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

    /// <summary>Configures tenant query filters behavior for this component.</summary>
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

    // DbSets
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<TaskItem> TaskItems { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<ChecklistItem> ChecklistItems { get; set; } = null!;
    public DbSet<Attachment> Attachments { get; set; } = null!;
    public DbSet<TaskItemTag> TaskItemTags { get; set; } = null!;
}
