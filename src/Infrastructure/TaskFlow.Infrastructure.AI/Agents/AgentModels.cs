namespace TaskFlow.Infrastructure.AI.Agents;

public class AgentChatRequest
{
    public string Message { get; set; } = null!;
    public string? ConversationId { get; set; }
}

public class AgentChatResponse
{
    public string Message { get; set; } = null!;
    public string ConversationId { get; set; } = null!;
    public bool IsConfigured { get; set; }
}
