using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
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
        AddRateLimiting(services);
        AddOpenApi(services, config);

        return services;
    }

    private static void AddJsonOptions(IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    }

    private static void AddCors(IServiceCollection services, IConfiguration config)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("TaskFlowUi", policy =>
                policy.WithOrigins(
                        "https://localhost:55551",
                        "http://localhost:55553",
                        "https://localhost:7067",
                        "http://localhost:5188")
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

    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("PerTenant", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User?.FindFirst("tenant_id")?.Value ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
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
