using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Bootstrapper.HealthChecks;

public sealed class MemoryHealthCheck(IConfiguration config) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var thresholdBytes = config.GetValue<long>(
            "MemoryHealthCheckBytesThreshold",
            1024L * 1024L * 1024L);
        var totalMemoryBytes = GC.GetTotalMemory(forceFullCollection: false);

        if (totalMemoryBytes <= thresholdBytes)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "Memory usage is below the configured threshold.",
                new Dictionary<string, object> { ["allocatedBytes"] = totalMemoryBytes }));
        }

        return Task.FromResult(HealthCheckResult.Degraded(
            "Memory usage is above the configured threshold.",
            data: new Dictionary<string, object> { ["allocatedBytes"] = totalMemoryBytes }));
    }
}
