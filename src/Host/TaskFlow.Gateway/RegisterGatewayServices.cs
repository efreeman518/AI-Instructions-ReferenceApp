using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using TaskFlow.Gateway.HealthChecks;
using Yarp.ReverseProxy.Transforms;

namespace TaskFlow.Gateway;

/// <summary>
/// Gateway composition root. It validates external users, forwards original user claims to the
/// API, acquires downstream service tokens, applies CORS, health checks, rate limiting, and YARP.
/// </summary>
public static class RegisterGatewayServices
{
    /// <summary>
    /// Registers gateway-only services. The API remains the authorization and business boundary;
    /// the gateway handles edge auth and downstream token exchange.
    /// </summary>
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
        services.AddSingleton<TokenService>();
        services.AddHeaderPropagation(options => options.Headers.Add("X-Correlation-Id"));
        AddAuthentication(services, config);
        AddReverseProxy(services, config);
        AddCors(services, config);
        AddHealthChecks(services, config);
        AddRateLimiting(services, config);
        return services;
    }

    /// <summary>Registers authentication dependencies in the service container.</summary>
    private static void AddAuthentication(IServiceCollection services, IConfiguration config)
    {
        var entraSection = config.GetSection("EntraExternal");
        if (!entraSection.Exists() || string.IsNullOrWhiteSpace(entraSection["ClientId"]))
        {
            // Dev mode: register no-op auth so middleware doesn't reject requests
            services.AddAuthentication().AddJwtBearer(options =>
            {
                // No validation - scaffold passthrough
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

    /// <summary>Registers reverse proxy dependencies in the service container.</summary>
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

                    // Forward original user claims as X-Orig-Request header for the API
                    // claims transformer after the API validates the gateway service token.
                    AddOriginalUserClaimsHeader(ctx);

                    // Acquire service token for downstream API
                    var token = await tokenService.GetAccessTokenAsync(clusterId, ctx.HttpContext.RequestAborted);
                    ctx.ProxyRequest!.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                });
            });
    }

    /// <summary>Registers original user claims header dependencies in the service container.</summary>
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

    /// <summary>Registers cors dependencies in the service container.</summary>
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

    /// <summary>Registers health checks dependencies in the service container.</summary>
    private static void AddHealthChecks(IServiceCollection services, IConfiguration config)
    {
        services.Configure<AggregateHealthCheckSettings>(
            config.GetSection(AggregateHealthCheckSettings.ConfigSectionName));

        services.AddHttpClient(nameof(AggregateGatewayHealthCheck));

        services.AddHealthChecks()
            .AddCheck<AggregateGatewayHealthCheck>("taskflow-api", tags: ["full", "extservice"]);
    }

    /// <summary>Registers rate limiting dependencies in the service container.</summary>
    private static void AddRateLimiting(IServiceCollection services, IConfiguration config)
    {
        var memoryPermitLimit = config.GetValue<int?>("RateLimiting:Health:MemoryPermitLimit") ?? 30;
        var fullPermitLimit = config.GetValue<int?>("RateLimiting:Health:FullPermitLimit") ?? 3;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("HealthMemory", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = memoryPermitLimit,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            options.AddPolicy("HealthFull", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = fullPermitLimit,
                        Window = TimeSpan.FromSeconds(30),
                        QueueLimit = 1,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });
    }

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    private static string GetRequiredValue(IConfigurationSection section, string key) =>
        section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Missing configuration value '{section.Path}:{key}'.");
}
