using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Bootstrapper.HealthChecks;

public sealed class BlobStorageHealthCheck(
    IAzureClientFactory<BlobServiceClient> clientFactory) : IHealthCheck
{
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
