using EF.FlowEngine.Outbox.Sql;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Infrastructure.Data;

// Inherits FlowEngineOutboxDbContext (which itself extends FlowEngineDbContext) so the same
// DbContext owns execution-state + outbox tables — required for SqlExecutionStateStore's
// atomic save+enqueue path via IOutboxCapableDbContext.
//
// Co-located with TaskFlowDbContextTrxn on the same SQL Server connection but in its own
// `flowengine` schema with an isolated migration history table to avoid colliding with
// the application's own migrations.
public sealed class TaskFlowFlowEngineDbContext(DbContextOptions<TaskFlowFlowEngineDbContext> options)
    : FlowEngineOutboxDbContext(options)
{
    public const string SchemaName = "flowengine";
    public const string MigrationHistoryTable = "__EFMigrationsHistory_FlowEngine";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);
    }
}
