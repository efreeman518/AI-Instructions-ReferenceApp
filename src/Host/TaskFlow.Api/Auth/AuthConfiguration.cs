using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace TaskFlow.Api.Auth;

public static class AuthConfiguration
{
    /// <summary>
    /// Registers authentication based on AuthMode config.
    /// "Scaffold" → ScaffoldAuthHandler (no external provider needed).
    /// "EntraID" → JWT Bearer with Entra ID validation.
    /// </summary>
    public static IServiceCollection AddTaskFlowAuth(this IServiceCollection services, IConfiguration config)
    {
        var mode = config["AuthMode"] ?? "Scaffold";

        if (mode.Equals("Scaffold", StringComparison.OrdinalIgnoreCase))
        {
            services.AddAuthentication(ScaffoldAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, ScaffoldAuthHandler>(
                    ScaffoldAuthHandler.SchemeName, _ => { });
            return services;
        }

        // Production: Entra ID JWT Bearer
        var section = config.GetSection("EntraID");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var tenantId = section["TenantId"];
                var instance = section["Instance"] ?? "https://login.microsoftonline.com/";

                options.Authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
                options.Audience = section["ClientId"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{instance.TrimEnd('/')}/{tenantId}/v2.0",
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    RoleClaimType = "roles",
                    NameClaimType = "name"
                };
            });

        return services;
    }
}
