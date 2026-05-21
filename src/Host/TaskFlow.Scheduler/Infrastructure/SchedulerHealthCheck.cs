using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Scheduler.Infrastructure;

public sealed class SchedulerHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("Scheduler host is running."));
}
