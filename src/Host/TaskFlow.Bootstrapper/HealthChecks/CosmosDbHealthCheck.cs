using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Bootstrapper.HealthChecks;

public sealed class CosmosDbHealthCheck(CosmosClient client) : IHealthCheck
{
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
