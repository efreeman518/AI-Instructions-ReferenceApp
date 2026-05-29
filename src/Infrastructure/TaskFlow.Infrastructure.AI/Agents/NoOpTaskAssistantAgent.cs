using Microsoft.Extensions.Logging;

namespace TaskFlow.Infrastructure.AI.Agents;

/// <summary>Provides no op task assistant agent behavior for the Infrastructure Agents layer.</summary>
public class NoOpTaskAssistantAgent(ILogger<NoOpTaskAssistantAgent> logger) : ITaskAssistantAgent
{
    /// <summary>Provides the chat operation for no op task assistant agent.</summary>
    public Task<AgentChatResponse> ChatAsync(
        AgentChatRequest request, Guid? tenantId, CancellationToken ct = default)
    {
        logger.LogWarning("TaskAssistant agent not configured - returning stub response");
        return Task.FromResult(new AgentChatResponse
        {
            Message = "AI agent is not configured. Set the AiServices:FoundryEndpoint configuration to enable.",
            ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
            IsConfigured = false
        });
    }
}
