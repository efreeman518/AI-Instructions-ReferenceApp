using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Bootstrapper.HealthChecks;

public class SqlHealthCheck(IDbContextFactory<TaskFlowDbContextTrxn> factory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var db = await factory.CreateDbContextAsync(ct);
            await db.Database.CanConnectAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL connection failed", ex);
        }
    }
}
