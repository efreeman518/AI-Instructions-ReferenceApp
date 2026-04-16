using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Bootstrapper.StartupTasks;

public class WarmupDependencies(
    IDbContextFactory<TaskFlowDbContextTrxn> trxnFactory,
    IDbContextFactory<TaskFlowDbContextQuery> queryFactory,
    ILogger<WarmupDependencies> logger) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            await using var trxnDb = await trxnFactory.CreateDbContextAsync(ct);
            await trxnDb.Database.CanConnectAsync(ct);

            await using var queryDb = await queryFactory.CreateDbContextAsync(ct);
            await queryDb.Database.CanConnectAsync(ct);

            logger.LogInformation("Database warmup completed");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database warmup failed — continuing startup");
        }
    }
}
