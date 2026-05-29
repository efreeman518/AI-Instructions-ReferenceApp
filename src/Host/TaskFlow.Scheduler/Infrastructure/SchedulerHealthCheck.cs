using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Scheduler.Infrastructure;

/// <summary>Configures scheduler health check host behavior for TaskFlow runtime services.</summary>
public sealed class SchedulerHealthCheck : IHealthCheck
{
    /// <summary>Provides the check health operation for scheduler health check.</summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("Scheduler host is running."));
}
