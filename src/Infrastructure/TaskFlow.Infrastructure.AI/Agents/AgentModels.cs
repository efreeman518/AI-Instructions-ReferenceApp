namespace TaskFlow.Infrastructure.AI.Agents;

/// <summary>Carries agent chat request CQRS data between endpoints and handlers.</summary>
public class AgentChatRequest
{
    public string Message { get; set; } = null!;
    public string? ConversationId { get; set; }
}

/// <summary>Provides agent chat response behavior for the Infrastructure Agents layer.</summary>
public class AgentChatResponse
{
    public string Message { get; set; } = null!;
    public string ConversationId { get; set; } = null!;
    public bool IsConfigured { get; set; }
}
