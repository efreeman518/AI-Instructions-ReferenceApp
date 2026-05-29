using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Bootstrapper.HealthChecks;

/// <summary>Configures service bus health check host behavior for TaskFlow runtime services.</summary>
public sealed class ServiceBusHealthCheck(IConfiguration config) : IHealthCheck
{
    /// <summary>Provides the check health operation for service bus health check.</summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = config.GetConnectionString("ServiceBus1")
            ?? config["ServiceBus1"]
            ?? config["Values:ServiceBus1"];
        if (string.IsNullOrWhiteSpace(connectionString))
            return HealthCheckResult.Healthy("Service Bus connection string not configured; skipping probe.");

        try
        {
            var client = new ServiceBusAdministrationClient(connectionString);
            await client.GetNamespacePropertiesAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Service Bus connection failed.", ex);
        }
    }
}
