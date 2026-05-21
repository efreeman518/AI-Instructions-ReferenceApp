using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using TaskFlow.Api.Auth;
using TaskFlow.Api.Middleware;

namespace TaskFlow.Api;

public static class RegisterApiServices
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services, IConfiguration config, ILogger startupLogger)
    {
        services.AddHttpContextAccessor();
        AddJsonOptions(services);
        AddCors(services, config);
        AddAuthentication(services, config, startupLogger);
        AddAuthorization(services);
        AddExceptionHandling(services);
        AddCorrelationTracking(services);
        AddRateLimiting(services, config);
        AddApiVersioning(services);
        AddOpenApi(services, config);

        // Workflow JSON seeding is now configured in the bootstrapper via
        // FlowEngineBuilder.AddWorkflowJsonSeeding (EF.FlowEngine v1.0.104+).
        // The seeding hosted service auto-discovers ./Workflows at startup.

        return services;
    }

    private static void AddJsonOptions(IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    }

    private static void AddCors(IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("CORS is not configured. Set Cors:AllowedOrigins in configuration.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy("TaskFlowUi", policy =>
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration config, ILogger logger)
    {
        services.AddTaskFlowAuth(config);
        services.Configure<GatewayClaimsTransformSettings>(
            config.GetSection(GatewayClaimsTransformSettings.ConfigSectionName));
        services.AddTransient<IClaimsTransformation, GatewayClaimsTransformer>();
    }

    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddTaskFlowAuthorization();
    }

    private static void AddExceptionHandling(IServiceCollection services)
    {
        services.AddExceptionHandler<DefaultExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);

                var activity = Activity.Current;
                if (!string.IsNullOrWhiteSpace(activity?.Id))
                    context.ProblemDetails.Extensions.TryAdd("activityId", activity.Id);
            };
        });
    }

    private static void AddCorrelationTracking(IServiceCollection services)
    {
        services.AddHeaderPropagation(options =>
        {
            options.Headers.Add(CorrelationIdMiddleware.HeaderName);
        });
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration config)
    {
        var permitLimit = config.GetValue<int?>("RateLimiting:PerTenant:PermitLimit") ?? 100;
        var windowSeconds = config.GetValue<int?>("RateLimiting:PerTenant:WindowSeconds") ?? 60;
        var healthMemoryPermitLimit = config.GetValue<int?>("RateLimiting:Health:MemoryPermitLimit") ?? 30;
        var healthDbPermitLimit = config.GetValue<int?>("RateLimiting:Health:DbPermitLimit") ?? 6;
        var healthFullPermitLimit = config.GetValue<int?>("RateLimiting:Health:FullPermitLimit") ?? 3;

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (context.Request.Path.StartsWithSegments("/health")
                    || context.Request.Path.StartsWithSegments("/alive")
                    || context.Request.Path.StartsWithSegments("/healthz")
                    || context.Request.Path.StartsWithSegments("/readyz"))
                    return RateLimitPartition.GetNoLimiter("health");

                return RateLimitPartition.GetFixedWindowLimiter(
                    context.User?.FindFirst("tenant_id")?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy("PerTenant", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User?.FindFirst("tenant_id")?.Value ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0
                    }));

            options.AddPolicy("HealthMemory", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = healthMemoryPermitLimit,
                    Window = TimeSpan.FromSeconds(10),
                    QueueLimit = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

            options.AddPolicy("HealthDb", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = healthDbPermitLimit,
                    Window = TimeSpan.FromSeconds(10),
                    QueueLimit = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

            options.AddPolicy("HealthFull", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = healthFullPermitLimit,
                    Window = TimeSpan.FromSeconds(30),
                    QueueLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }

    private static IApiVersioningBuilder AddApiVersioning(IServiceCollection services)
    {
        return services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = ApiContract.DefaultVersion;
            options.AssumeDefaultVersionWhenUnspecified = false;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = ApiContract.ApiExplorerGroupNameFormat;
            options.SubstituteApiVersionInUrl = true;
        });
    }

    private static void AddOpenApi(
        IServiceCollection services,
        IConfiguration config)
    {
        if (!config.GetValue<bool>("OpenApiSettings:Enable", true)) return;

        foreach (var apiDocument in ApiContract.SupportedDocuments)
        {
            services.AddOpenApi(apiDocument.GroupName, options =>
            {
                options.ShouldInclude = apiDescription =>
                    string.Equals(apiDescription.GroupName, apiDocument.GroupName, StringComparison.OrdinalIgnoreCase);

                options.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Info = new()
                    {
                        Title = ApiContract.Title,
                        Version = apiDocument.DisplayName,
                        Description = ApiContract.Description
                    };
                    return Task.CompletedTask;
                });
            });
        }
    }
}
