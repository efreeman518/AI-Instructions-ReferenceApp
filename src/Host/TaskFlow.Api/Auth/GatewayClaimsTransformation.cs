using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace TaskFlow.Api.Auth;

/// <summary>
/// Middleware that reads X-Orig-Request header forwarded by the Gateway and enriches
/// the authenticated principal with the original user claims.
/// </summary>
public sealed class GatewayClaimsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayClaimsMiddleware> _logger;

    public GatewayClaimsMiddleware(RequestDelegate next, ILogger<GatewayClaimsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers["X-Orig-Request"].FirstOrDefault();
        if (!string.IsNullOrEmpty(header) && context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(header));
                var claims = JsonSerializer.Deserialize<ForwardedClaims>(json);

                if (claims is not null)
                {
                    var identity = context.User.Identity as ClaimsIdentity ?? new ClaimsIdentity();

                    AddClaimIfMissing(identity, "oid", claims.Sub);
                    AddClaimIfMissing(identity, ClaimTypes.NameIdentifier, claims.Sub);
                    AddClaimIfMissing(identity, "tenant_id", claims.TenantId);
                    AddClaimIfMissing(identity, ClaimTypes.Name, claims.Name);

                    if (claims.Roles is { Length: > 0 })
                    {
                        foreach (var role in claims.Roles)
                        {
                            AddClaimIfMissing(identity, ClaimTypes.Role, role);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse X-Orig-Request header");
            }
        }

        await _next(context);
    }

    private static void AddClaimIfMissing(ClaimsIdentity identity, string type, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (!identity.HasClaim(c => c.Type == type && c.Value == value))
            identity.AddClaim(new Claim(type, value));
    }

    private sealed record ForwardedClaims(
        string? Sub,
        string? TenantId,
        string? Name,
        string[]? Roles)
    {
        // Support case-insensitive JSON deserialization
        [System.Text.Json.Serialization.JsonPropertyName("sub")]
        public string? Sub { get; init; } = Sub;

        [System.Text.Json.Serialization.JsonPropertyName("tenant_id")]
        public string? TenantId { get; init; } = TenantId;

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; init; } = Name;

        [System.Text.Json.Serialization.JsonPropertyName("roles")]
        public string[]? Roles { get; init; } = Roles;
    }
}
