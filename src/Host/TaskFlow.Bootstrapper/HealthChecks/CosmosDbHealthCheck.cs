using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Bootstrapper.HealthChecks;

/// <summary>Configures cosmos DB health check host behavior for TaskFlow runtime services.</summary>
public sealed class CosmosDbHealthCheck(CosmosClient client) : IHealthCheck
{
    /// <summary>Provides the check health operation for cosmos DB health check.</summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await client.ReadAccountAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cosmos DB connection failed.", ex);
        }
    }
}
