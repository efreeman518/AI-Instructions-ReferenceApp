using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Yarp.ReverseProxy.Transforms;

namespace TaskFlow.Gateway;

public static class RegisterGatewayServices
{
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<TokenService>();
        AddReverseProxy(services, config);
        AddCors(services);
        return services;
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
            sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("oid")?.Value,
            tenant_id = user.FindFirst("tenant_id")?.Value,
            name = user.FindFirst(ClaimTypes.Name)?.Value,
            roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        };

        var json = JsonSerializer.Serialize(claimsPayload);
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        ctx.ProxyRequest!.Headers.TryAddWithoutValidation("X-Orig-Request", encoded);
    }

    private static void AddCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("UnoUI", policy =>
            {
                policy.WithOrigins(
                        "https://localhost:5002",
                        "http://localhost:5002")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    }
}
