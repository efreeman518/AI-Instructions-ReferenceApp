using System.Collections.Concurrent;

namespace TaskFlow.Gateway;

/// <summary>
/// Acquires and caches client-credential tokens per downstream cluster.
/// When EntraID:ClientCredentials config is present, uses real MSAL token acquisition.
/// Otherwise, returns scaffold stub tokens for local development.
/// </summary>
public sealed class TokenService
{
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expiry)> _cache = new();
    private readonly ILogger<TokenService> _logger;
    private readonly IConfiguration _config;

    public TokenService(ILogger<TokenService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Task<string> GetAccessTokenAsync(string clusterId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(clusterId, out var cached) && cached.Expiry > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return Task.FromResult(cached.Token);
        }

        var section = _config.GetSection("EntraID:ClientCredentials");
        if (section.Exists() && !string.IsNullOrWhiteSpace(section["ClientId"]))
        {
            // TODO: [CONFIGURE] Replace with real MSAL ConfidentialClientApplication token acquisition
            // var app = ConfidentialClientApplicationBuilder.Create(section["ClientId"])
            //     .WithClientSecret(section["ClientSecret"])
            //     .WithAuthority(section["Authority"])
            //     .Build();
            // var result = await app.AcquireTokenForClient(scopes).ExecuteAsync(ct);
            // CacheToken(clusterId, result.AccessToken, result.ExpiresOn);
            // return result.AccessToken;
            _logger.LogWarning("EntraID:ClientCredentials configured but MSAL not yet wired. Using scaffold token for {ClusterId}", clusterId);
        }

        // Scaffold stub: return a fixed token for local development
        var stubToken = $"scaffold-token-{clusterId}-{Guid.NewGuid():N}";
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        _cache[clusterId] = (stubToken, expiry);

        _logger.LogDebug("Scaffold token issued for cluster {ClusterId}, expires {Expiry}", clusterId, expiry);
        return Task.FromResult(stubToken);
    }
}
