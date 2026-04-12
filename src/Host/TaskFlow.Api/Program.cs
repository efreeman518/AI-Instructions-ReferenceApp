using System.Threading.RateLimiting;
using TaskFlow.Api.Auth;
using TaskFlow.Api.Endpoints;
using TaskFlow.Api.Middleware;
using TaskFlow.Bootstrapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTaskFlowServices(builder.Configuration);

// Authentication + Authorization
builder.Services.AddTaskFlowAuth(builder.Configuration);
builder.Services.AddTaskFlowAuthorization();

// Global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("PerTenant", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User?.FindFirst("tenant_id")?.Value ?? "anonymous",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// Middleware pipeline (order matters)
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<GatewayClaimsMiddleware>();
app.UseAuthorization();

app.MapDefaultEndpoints();

// API endpoint groups
app.MapCategoryEndpoints();
app.MapTagEndpoints();
app.MapTaskItemEndpoints();
app.MapCommentEndpoints();
app.MapChecklistItemEndpoints();
app.MapAttachmentEndpoints();
app.MapTaskItemTagEndpoints();

app.Run();
