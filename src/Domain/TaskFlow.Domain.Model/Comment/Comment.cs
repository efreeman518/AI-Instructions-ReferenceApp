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

    private Comment() { }

    private Comment(Guid tenantId, Guid taskItemId, string body)
    {
        TenantId = tenantId;
        TaskItemId = taskItemId;
        Body = body;
    }

    public static DomainResult<Comment> Create(Guid tenantId, Guid taskItemId, string body)
    {
        var entity = new Comment(tenantId, taskItemId, body);
        return entity.Valid();
    }

    public DomainResult<Comment> Update(string? body = null)
    {
        if (body is not null) Body = body;
        return Valid();
    }

    private DomainResult<Comment> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (TaskItemId == Guid.Empty) errors.Add(DomainError.Create("Task Item ID cannot be empty."));
        if (string.IsNullOrWhiteSpace(Body)) errors.Add(DomainError.Create("Body is required."));
        return errors.Count > 0
            ? DomainResult<Comment>.Failure(errors)
            : DomainResult<Comment>.Success(this);
    }
}
