namespace TaskFlow.Infrastructure.AI.Demos;

/// <summary>Request for the raw chat and streaming chat demos (D1/D2).</summary>
public record AiChatRequest(string Message);

/// <summary>Response for the raw (non-streaming) chat demo (D1).</summary>
public record AiChatResponse(string Message, bool IsConfigured);

/// <summary>Response for the no-call AI configuration status endpoint.</summary>
public record AiStatusResponse(string Provider, bool IsConfigured);

/// <summary>Structured triage classification produced by the model (D4).</summary>
public record TaskTriageResult(
    string SuggestedPriority,
    string? SuggestedCategory,
    double Confidence,
    string? Rationale);

/// <summary>Result envelope for the triage demo (D4).</summary>
public record TaskTriageResponse(
    Guid TaskId,
    TaskTriageResult? Triage,
    bool Applied,
    bool IsConfigured,
    string? Error = null);

/// <summary>Request for the AI-assisted task draft demo (D5).</summary>
public record DraftTaskRequest(string Title);

/// <summary>Result envelope for the AI-assisted task draft demo (D5).</summary>
public record DraftTaskResponse(
    Guid? TaskId,
    string Title,
    string? Description,
    string? AcceptanceCriteria,
    bool Created,
    bool IsConfigured,
    string? Error = null);

/// <summary>Result envelope for the read-only next-action advisor demo (D7).</summary>
public record NextActionResponse(string Recommendation, bool IsConfigured);
