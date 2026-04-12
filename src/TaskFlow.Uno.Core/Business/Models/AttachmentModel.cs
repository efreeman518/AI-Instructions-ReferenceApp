namespace TaskFlow.Uno.Core.Business.Models;

public record AttachmentModel
{
    public Guid? Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string StorageUri { get; init; } = string.Empty;
    public string OwnerType { get; init; } = "TaskItem";
    public Guid OwnerId { get; init; }
}
