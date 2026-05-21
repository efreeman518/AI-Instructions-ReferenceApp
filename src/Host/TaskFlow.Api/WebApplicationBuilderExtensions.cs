using EF.AspNetCore.Correlation;
using EF.AspNetCore.Security;
using EF.AspNetCore.Versioning;
using EF.FlowEngine.AdminApi;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using TaskFlow.Api.Endpoints;
using TaskFlow.Api.Endpoints.Cqrs;
using TaskFlow.Application.Contracts;

namespace TaskFlow.Api;

public static class WebApplicationBuilderExtensions
{
    private static bool _problemDetailsIncludeStackTrace;

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        _problemDetailsIncludeStackTrace = app.Environment.IsDevelopment() || app.Environment.IsStaging();

        // 1. Security headers
        app.UseBasicSecurityHeaders();

        // 2. Correlation tracking
        app.UseCorrelationId();
        app.UseHeaderPropagation();

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
            app.MapOpenApi()
                .AllowAnonymous();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle(ApiContract.Title);
                options.WithTheme(ScalarTheme.Moon);
            })
            .AllowAnonymous();
        }

        // Default Aspire endpoints
        app.MapDefaultEndpoints();

        // Health endpoints
        app.MapHealthChecks("/health/memory", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("memory")
        })
        .AllowAnonymous()
        .RequireRateLimiting("HealthMemory");

        app.MapHealthChecks("/health/db", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db")
        })
        .RequireAuthorization()
        .RequireRateLimiting("HealthDb");

        app.MapHealthChecks("/health/full", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("full")
        })
        .RequireAuthorization()
        .RequireRateLimiting("HealthFull");

        app.MapGet("/alive", () => Results.Ok("Alive"))
            .AllowAnonymous()
            .RequireRateLimiting("HealthMemory");

        // API endpoint groups
        SetupApiEndpoints(app);

        // FlowEngine admin API - instance/registry/circuit-breaker/human-task operations.
        // Fronted by YARP gateway; consumed by EF.FlowEngine.Dashboard hosted in TaskFlow.Blazor.
        app.MapFlowEngineAdmin(prefix: "/api/flowengine");

        return app;
    }

    public static bool ProblemDetailsIncludeStackTrace => _problemDetailsIncludeStackTrace;

    private static void SetupApiEndpoints(WebApplication app)
    {
        var apiDocuments = ApiContract.SupportedDocuments
            .Select(apiDocument => new ApiVersionDocument(apiDocument.Version, apiDocument.GroupName)
            {
                DisplayName = apiDocument.DisplayName
            })
            .ToArray();

        var versionSet = app.BuildApiVersionSet(apiDocuments);
        var api = app.MapVersionedApiGroup(ApiContract.VersionedRoutePrefix, versionSet, ApiContract.DefaultVersion)
            .RequireRateLimiting("PerTenant");

        var style = ApplicationStyleResolver.Resolve(app.Configuration[ApplicationStyleResolver.ConfigKey]);
        if (style == ApplicationStyle.Cqrs)
        {
            api.MapCategoryCqrsEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapTagCqrsEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapTaskItemCqrsEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapCommentCqrsEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapChecklistItemCqrsEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapAttachmentCqrsEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapTaskItemTagCqrsEndpoints(ProblemDetailsIncludeStackTrace);
        }
        else
        {
            api.MapCategoryEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapTagEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapTaskItemEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapCommentEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapChecklistItemEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapAttachmentEndpoints(ProblemDetailsIncludeStackTrace);
            api.MapTaskItemTagEndpoints(ProblemDetailsIncludeStackTrace);
        }

        api.MapSearchEndpoints();
        api.MapAgentEndpoints();
        api.MapTaskViewEndpoints();
    }
}
