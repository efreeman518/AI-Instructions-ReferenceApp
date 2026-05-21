using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace TaskFlow.Gateway.HealthChecks;

public sealed class AggregateHealthCheckSettings
{
    public const string ConfigSectionName = "AggregateHealthCheck";

    public string? TaskFlowApiHealthUrl { get; set; }
    public string? TaskFlowApiClusterId { get; set; }
    public int TimeoutSeconds { get; set; } = 5;
}

public sealed class AggregateGatewayHealthCheck(
    IOptions<AggregateHealthCheckSettings> options,
    TokenService tokenService,
    IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.TaskFlowApiHealthUrl))
            return HealthCheckResult.Healthy("TaskFlow API health URL not configured; skipping downstream probe.");

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));

            using var request = new HttpRequestMessage(HttpMethod.Get, settings.TaskFlowApiHealthUrl);
            if (!string.IsNullOrWhiteSpace(settings.TaskFlowApiClusterId))
            {
                var token = await tokenService.GetAccessTokenAsync(settings.TaskFlowApiClusterId, timeout.Token);
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var client = httpClientFactory.CreateClient(nameof(AggregateGatewayHealthCheck));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("TaskFlow API reachable.")
                : HealthCheckResult.Degraded($"TaskFlow API health returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("TaskFlow API health unreachable.", ex);
        }
    }
}
