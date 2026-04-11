namespace TaskFlow.Application.Models;

public class CommentDto
{
    public Guid? Id { get; set; }
    public string Body { get; set; } = null!;
    public Guid TaskItemId { get; set; }
    public List<AttachmentDto>? Attachments { get; set; }
}
