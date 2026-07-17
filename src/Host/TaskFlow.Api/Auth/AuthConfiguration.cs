using Microsoft.AspNetCore.Authentication;
using TaskFlow.Application.Contracts;

namespace TaskFlow.Api.Auth;

/// <summary>
/// Config-driven API authentication. TaskFlow supports scaffold mode so the reference app runs
/// with a predictable identity and no interactive or external identity-provider dependency.
/// </summary>
public static class AuthConfiguration
{
    /// <summary>
    /// Registers the scaffold authentication handler after validating AuthMode.
    /// </summary>
    public static IServiceCollection AddTaskFlowAuth(this IServiceCollection services, IConfiguration config)
    {
        _ = AuthModeResolver.Resolve(config[AuthModeResolver.ConfigKey]);

        services.AddAuthentication(ScaffoldAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ScaffoldAuthHandler>(
                ScaffoldAuthHandler.SchemeName, _ => { });
        return services;
    }
}
