using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.Utilities.Entities;

namespace TaskFlow.Infrastructure.Data;

/// <summary>
/// EF tooling and migrator context for TickerQ operational tables.
/// Scheduler runtime uses the same model but never creates or patches this schema at startup.
/// </summary>
public sealed class TaskFlowTickerQDbContext(DbContextOptions<TaskFlowTickerQDbContext> options)
    : TickerQDbContext<TimeTickerEntity, CronTickerEntity>(options)
{
    public const string SchemaName = "Scheduler";
    public const string MigrationHistoryTable = "__EFMigrationsHistory_TickerQ";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TimeTickerEntity>().ToTable("TimeTickers", SchemaName);
        modelBuilder.Entity<CronTickerEntity>().ToTable("CronTickers", SchemaName);
        modelBuilder.Entity<CronTickerOccurrenceEntity<CronTickerEntity>>()
            .ToTable("CronTickerOccurrences", SchemaName);
    }
}
