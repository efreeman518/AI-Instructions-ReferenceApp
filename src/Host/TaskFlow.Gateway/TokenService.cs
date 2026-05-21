using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;

namespace TaskFlow.Gateway;

/// <summary>
/// Acquires and caches client-credential tokens per downstream cluster.
/// Injects TokenCredential (DefaultAzureCredential) for real token acquisition.
/// Falls back to scaffold stub tokens when no credential is configured.
/// </summary>
public sealed class TokenService
{
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expiry)> _cache = new();
    private readonly ILogger<TokenService> _logger;
    private readonly TokenCredential _credential;
    private readonly IConfiguration _config;

    public TokenService(ILogger<TokenService> logger, TokenCredential credential, IConfiguration config)
    {
        _logger = logger;
        _credential = credential;
        _config = config;
    }

    public async Task<string> GetAccessTokenAsync(string clusterId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(clusterId, out var cached) && cached.Expiry > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return cached.Token;
        }

        var scopeSection = _config.GetSection($"ReverseProxy:Clusters:{clusterId}:TokenScope");
        var scope = scopeSection.Value;

        if (!string.IsNullOrWhiteSpace(scope))
        {
            var tokenResult = await _credential.GetTokenAsync(
                new TokenRequestContext([scope]), ct);
            _cache[clusterId] = (tokenResult.Token, tokenResult.ExpiresOn);
            _logger.LogDebug("Token acquired for cluster {ClusterId}, expires {Expiry}", clusterId, tokenResult.ExpiresOn);
            return tokenResult.Token;
        }

        // Scaffold stub: return a fixed token for local development
        var stubToken = $"scaffold-token-{clusterId}-{Guid.NewGuid():N}";
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        _cache[clusterId] = (stubToken, expiry);

        _logger.LogDebug("Scaffold token issued for cluster {ClusterId}, expires {Expiry}", clusterId, expiry);
        return stubToken;
    }
}
