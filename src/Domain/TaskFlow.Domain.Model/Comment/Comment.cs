using EF.Domain;
using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model;

public class Comment : EntityBase, ITenantEntity<Guid>
{
    public Guid TenantId { get; init; }
    public string Body { get; private set; } = null!;

    // Foreign key
    public Guid TaskItemId { get; private set; }

    // Navigation
    public TaskItem TaskItem { get; private set; } = null!;
    public ICollection<Attachment> Attachments { get; private set; } = [];

    private Comment() { }

    public static DomainResult<Comment> Create(Guid tenantId, Guid taskItemId, string body)
        => throw new NotImplementedException("Shell — implement in Phase 5a");

    public DomainResult<Comment> Update(string? body = null)
        => throw new NotImplementedException("Shell — implement in Phase 5a");
}
