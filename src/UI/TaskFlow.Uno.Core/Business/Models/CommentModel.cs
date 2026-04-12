namespace TaskFlow.Uno.Core.Business.Models;

public record CommentModel
{
    public Guid? Id { get; init; }
    public string Body { get; init; } = string.Empty;
    public Guid TaskItemId { get; init; }
    public IReadOnlyList<AttachmentModel>? Attachments { get; init; }
}
