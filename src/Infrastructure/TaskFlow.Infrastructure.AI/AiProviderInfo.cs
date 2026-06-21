namespace TaskFlow.Infrastructure.AI;

/// <summary>Records which AI provider won during host startup: azure, local, or none.</summary>
public sealed record AiProviderInfo(string Name);
