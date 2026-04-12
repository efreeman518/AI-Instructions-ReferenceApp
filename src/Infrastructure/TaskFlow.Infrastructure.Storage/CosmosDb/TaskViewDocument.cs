using Newtonsoft.Json;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Infrastructure.Storage.CosmosDb;

public class TaskViewDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = null!;

    [JsonProperty("title")]
    public string Title { get; set; } = null!;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = null!;

    [JsonProperty("priority")]
    public string Priority { get; set; } = null!;

    [JsonProperty("categoryName")]
    public string? CategoryName { get; set; }

    [JsonProperty("startDate")]
    public DateTimeOffset? StartDate { get; set; }

    [JsonProperty("dueDate")]
    public DateTimeOffset? DueDate { get; set; }

    [JsonProperty("completedDate")]
    public DateTimeOffset? CompletedDate { get; set; }

    [JsonProperty("isOverdue")]
    public bool IsOverdue { get; set; }

    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonProperty("commentCount")]
    public int CommentCount { get; set; }

    [JsonProperty("checklistTotal")]
    public int ChecklistTotal { get; set; }

    [JsonProperty("checklistCompleted")]
    public int ChecklistCompleted { get; set; }

    [JsonProperty("attachmentCount")]
    public int AttachmentCount { get; set; }

    [JsonProperty("subTaskCount")]
    public int SubTaskCount { get; set; }

    [JsonProperty("lastModifiedUtc")]
    public DateTimeOffset LastModifiedUtc { get; set; }

    [JsonProperty("createdUtc")]
    public DateTimeOffset CreatedUtc { get; set; }
}
