namespace TaskFlow.Infrastructure.AI.Agents;

/// <summary>Defines the task assistant agent contract used by TaskFlow components.</summary>
public interface ITaskAssistantAgent
{
    /// <summary>Provides the chat operation for task assistant agent.</summary>
    Task<AgentChatResponse> ChatAsync(
        AgentChatRequest request, Guid? tenantId, CancellationToken ct = default);
}
