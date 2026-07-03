using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskFlow.Uno.Core.Business.Notifications;

/// <summary>Represents or dispatches problem details state for the Uno client.</summary>
public sealed class ProblemDetailsPayload
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("detail")] public string? Detail { get; set; }
    [JsonPropertyName("instance")] public string? Instance { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}
