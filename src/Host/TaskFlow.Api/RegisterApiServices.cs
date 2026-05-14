using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using TaskFlow.Api.Auth;
using TaskFlow.Api.Middleware;
using TaskFlow.Api.Workflows;
using TaskFlow.Bootstrapper;

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
        AddRateLimiting(services, config);
        AddOpenApi(services, config);

        // Seed workflow JSON definitions into the FlowEngine registry after migrations apply.
        services.AddScoped<IStartupTask, WorkflowSeedStartupTask>();

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
    }

    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddTaskFlowAuthorization();
    }

    private static void AddExceptionHandling(IServiceCollection services)
    {
        services.AddExceptionHandler<DefaultExceptionHandler>();
        services.AddProblemDetails();
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration config)
    {
        var permitLimit = config.GetValue<int?>("RateLimiting:PerTenant:PermitLimit") ?? 100;
        var windowSeconds = config.GetValue<int?>("RateLimiting:PerTenant:WindowSeconds") ?? 60;

        services.AddRateLimiter(options =>
        {
            options.AddPolicy("PerTenant", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User?.FindFirst("tenant_id")?.Value ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds)
                    }));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }

    private static void AddOpenApi(IServiceCollection services, IConfiguration config)
    {
        if (!config.GetValue<bool>("OpenApiSettings:Enable", true)) return;

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, ct) =>
            {
                document.Info = new()
                {
                    Title = "TaskFlow API",
                    Version = "v1",
                    Description = "Multi-tenant TaskFlow API"
                };
                return Task.CompletedTask;
            });
        });
    }
}
