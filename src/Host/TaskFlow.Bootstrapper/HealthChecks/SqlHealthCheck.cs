using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Bootstrapper.HealthChecks;

/// <summary>Configures SQL health check host behavior for TaskFlow runtime services.</summary>
public class SqlHealthCheck(IDbContextFactory<TaskFlowDbContextTrxn> factory) : IHealthCheck
{
    /// <summary>Provides the check health operation for SQL health check.</summary>
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
