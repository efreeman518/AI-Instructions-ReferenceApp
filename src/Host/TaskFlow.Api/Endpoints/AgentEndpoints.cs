using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TaskFlow.Infrastructure.AI.Agents;

namespace TaskFlow.Api.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agent").WithTags("Agent");

        group.MapPost("/chat", async (
            [FromBody] AgentChatRequest request,
            [FromServices] ITaskAssistantAgent agent,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            Guid? tenantId = Guid.TryParse(tenantClaim, out var tid) ? tid : null;

            var response = await agent.ChatAsync(request, tenantId, ct);
            return Results.Ok(response);
        }).WithName("AgentChat");

        return app;
    }
}
