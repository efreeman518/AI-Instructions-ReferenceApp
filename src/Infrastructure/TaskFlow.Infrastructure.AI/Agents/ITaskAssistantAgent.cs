namespace TaskFlow.Infrastructure.AI.Agents;

public interface ITaskAssistantAgent
{
    Task<AgentChatResponse> ChatAsync(
        AgentChatRequest request, Guid? tenantId, CancellationToken ct = default);
}
