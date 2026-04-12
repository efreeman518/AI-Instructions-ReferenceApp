using Microsoft.Extensions.Logging;

namespace TaskFlow.Infrastructure.AI.Agents;

public class NoOpTaskAssistantAgent(ILogger<NoOpTaskAssistantAgent> logger) : ITaskAssistantAgent
{
    public Task<AgentChatResponse> ChatAsync(
        AgentChatRequest request, Guid? tenantId, CancellationToken ct = default)
    {
        logger.LogWarning("TaskAssistant agent not configured — returning stub response");
        return Task.FromResult(new AgentChatResponse
        {
            Message = "AI agent is not configured. Set the AiServices:FoundryEndpoint configuration to enable.",
            ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
            IsConfigured = false
        });
    }
}
