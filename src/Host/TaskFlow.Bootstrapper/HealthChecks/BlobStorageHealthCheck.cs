using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Bootstrapper.HealthChecks;

/// <summary>Configures blob storage health check host behavior for TaskFlow runtime services.</summary>
public sealed class BlobStorageHealthCheck(
    IAzureClientFactory<BlobServiceClient> clientFactory) : IHealthCheck
{
    /// <summary>Provides the check health operation for blob storage health check.</summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateClient("TaskFlowBlobClient");
            await client.GetPropertiesAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob storage connection failed.", ex);
        }
    }
}
