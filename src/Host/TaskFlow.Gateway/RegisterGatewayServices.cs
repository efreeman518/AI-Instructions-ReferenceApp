using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;

namespace TaskFlow.Gateway;

public static class RegisterGatewayServices
{
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
        services.AddSingleton<TokenService>();
        AddAuthentication(services, config);
        AddReverseProxy(services, config);
        AddCors(services, config);
        return services;
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration config)
    {
        var entraSection = config.GetSection("EntraExternal");
        if (!entraSection.Exists() || string.IsNullOrWhiteSpace(entraSection["ClientId"]))
        {
            // Dev mode: register no-op auth so middleware doesn't reject requests
            services.AddAuthentication().AddJwtBearer(options =>
            {
                // No validation — scaffold passthrough
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    RequireSignedTokens = false,
                    RequireExpirationTime = false,
                    SignatureValidator = (token, _) => new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token)
                };
            });
            return;
        }

        // Production: Entra External ID JWT Bearer
        var instance = GetRequiredValue(entraSection, "Instance");
        var tenantId = GetRequiredValue(entraSection, "TenantId");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
            options.Audience = GetRequiredValue(entraSection, "ClientId");
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
    }

    private static void AddReverseProxy(IServiceCollection services, IConfiguration config)
    {
        services.AddReverseProxy()
            .LoadFromConfig(config.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver()
            .AddTransforms(context =>
            {
                context.AddRequestTransform(async ctx =>
                {
                    var tokenService = ctx.HttpContext.RequestServices.GetRequiredService<TokenService>();
                    var clusterId = context.Cluster?.ClusterId ?? "api-cluster";

                    // Forward original user claims as X-Orig-Request header
                    AddOriginalUserClaimsHeader(ctx);

                    // Acquire service token for downstream API
                    var token = await tokenService.GetAccessTokenAsync(clusterId, ctx.HttpContext.RequestAborted);
                    ctx.ProxyRequest!.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                });
            });
    }

    private static void AddOriginalUserClaimsHeader(RequestTransformContext ctx)
    {
        var user = ctx.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) return;

        var claimsPayload = new
        {
            sub = user.FindFirst("oid")?.Value
               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value,
            tenant_id = user.FindFirst("tenant_id")?.Value,
            name = user.FindFirst(ClaimTypes.Name)?.Value,
            roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        };

        var json = JsonSerializer.Serialize(claimsPayload);
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        ctx.ProxyRequest!.Headers.TryAddWithoutValidation("X-Orig-Request", encoded);
    }

    private static void AddCors(IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
        if (origins is null || origins.Length == 0)
        {
            throw new InvalidOperationException("CORS is not configured. Set CorsSettings:AllowedOrigins in configuration.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy("UnoUI", policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    }

    private static string GetRequiredValue(IConfigurationSection section, string key) =>
        section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Missing configuration value '{section.Path}:{key}'.");
}
