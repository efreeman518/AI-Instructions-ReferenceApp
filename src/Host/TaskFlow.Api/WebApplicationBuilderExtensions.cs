using EF.FlowEngine.AdminApi;
using Scalar.AspNetCore;
using TaskFlow.Api.Endpoints;
using TaskFlow.Api.Middleware;

namespace TaskFlow.Api;

public static class WebApplicationBuilderExtensions
{
    private static bool _problemDetailsIncludeStackTrace;

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        _problemDetailsIncludeStackTrace = app.Environment.IsDevelopment() || app.Environment.IsStaging();

        // 1. Security headers
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // 2. Correlation tracking
        app.UseMiddleware<CorrelationIdMiddleware>();

        // 3. Exception handler (before routing)
        app.UseExceptionHandler();

        // 4. Rate limiter
        app.UseRateLimiter();

        // 5. CORS
        app.UseCors("TaskFlowUi");

        // 6. Authentication
        app.UseAuthentication();

        // 7. Authorization
        app.UseAuthorization();

        // OpenAPI / Scalar
        if (app.Configuration.GetValue<bool>("OpenApiSettings:Enable", true))
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("TaskFlow API");
                options.WithTheme(ScalarTheme.Moon);
            });
        }

        // Default Aspire endpoints
        app.MapDefaultEndpoints();

        // Health endpoints
        app.MapHealthChecks("/health");
        app.MapGet("/alive", () => Results.Ok("Alive"));

        // API endpoint groups
        SetupApiEndpoints(app);

        // FlowEngine admin API — instance/registry/circuit-breaker/human-task operations.
        // Fronted by YARP gateway; consumed by EF.FlowEngine.Dashboard hosted in TaskFlow.Blazor.
        app.MapFlowEngineAdmin(prefix: "/api/flowengine");

        return app;
    }

    public static bool ProblemDetailsIncludeStackTrace => _problemDetailsIncludeStackTrace;

    private static void SetupApiEndpoints(WebApplication app)
    {
        app.MapCategoryEndpoints(ProblemDetailsIncludeStackTrace);
        app.MapTagEndpoints(ProblemDetailsIncludeStackTrace);
        app.MapTaskItemEndpoints(ProblemDetailsIncludeStackTrace);
        app.MapCommentEndpoints(ProblemDetailsIncludeStackTrace);
        app.MapChecklistItemEndpoints(ProblemDetailsIncludeStackTrace);
        app.MapAttachmentEndpoints(ProblemDetailsIncludeStackTrace);
        app.MapTaskItemTagEndpoints(ProblemDetailsIncludeStackTrace);
        app.MapSearchEndpoints();
        app.MapAgentEndpoints();
        app.MapTaskViewEndpoints();
    }
}
