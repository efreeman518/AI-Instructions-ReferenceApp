using TaskFlow.Application.Contracts;

namespace TaskFlow.Gateway;

/// <summary>Maps the public, non-sensitive authentication-mode contract.</summary>
public static class AuthEndpoints
{
    /// <summary>Maps the anonymous runtime mode signal consumed by clients.</summary>
    public static IEndpointConventionBuilder MapAuthModeEndpoint(
        this IEndpointRouteBuilder endpoints,
        AuthMode mode) =>
        endpoints.MapGet("/auth/mode", () => Results.Ok(new { mode = mode.ToString() }))
            .AllowAnonymous();
}
