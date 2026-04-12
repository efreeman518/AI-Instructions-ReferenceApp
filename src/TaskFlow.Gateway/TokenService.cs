using System.Collections.Concurrent;

namespace TaskFlow.Gateway;

/// <summary>
/// Acquires and caches client-credential tokens per downstream cluster.
/// Phase 5f replaces stub with real Entra ID token acquisition.
/// </summary>
public sealed class TokenService
{
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expiry)> _cache = new();
    private readonly ILogger<TokenService> _logger;

    public TokenService(ILogger<TokenService> logger)
    {
        _logger = logger;
    }

    public Task<string> GetAccessTokenAsync(string clusterId, CancellationToken ct = default)
    {
        // Scaffold stub: return a fixed token for local development
        // Phase 5f: replace with real client-credential flow (MSAL / Azure.Identity)
        if (_cache.TryGetValue(clusterId, out var cached) && cached.Expiry > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return Task.FromResult(cached.Token);
        }

        var stubToken = $"scaffold-token-{clusterId}-{Guid.NewGuid():N}";
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        _cache[clusterId] = (stubToken, expiry);

        _logger.LogDebug("Token acquired for cluster {ClusterId}, expires {Expiry}", clusterId, expiry);
        return Task.FromResult(stubToken);
    }
}
