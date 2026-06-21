using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Models;

/// <summary>
/// Sparse JSON-merge-patch contract for a TaskItem: every field is nullable and a null means
/// "leave unchanged". Used by PATCH /api/v1/task-items/{id} for targeted partial updates (e.g. the
/// AI triage workflow applying just a suggested priority) without round-tripping the full aggregate
/// through PUT, which is a full replace and would require every required field.
/// </summary>
public record TaskItemPatchDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Priority? Priority { get; set; }
    public decimal? EstimatedEffort { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ParentTaskItemId { get; set; }
}
