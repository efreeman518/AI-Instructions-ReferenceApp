using EF.Domain;
using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model;

public sealed class Tag : EntityBase, ITenantEntity<Guid>, IEquatable<Tag>
{
    public Guid TenantId { get; init; }
    public string Name { get; private set; } = null!;
    public string? Color { get; private set; }

    // Navigation
    public ICollection<TaskItemTag> TaskItemTags { get; private set; } = [];

    private Tag() { }

    private Tag(Guid tenantId, string name, string? color)
    {
        TenantId = tenantId;
        Name = name;
        Color = color;
    }

    public static DomainResult<Tag> Create(Guid tenantId, string name, string? color = null)
    {
        var entity = new Tag(tenantId, name, color);
        return entity.Valid();
    }

    public DomainResult<Tag> Update(string? name = null, string? color = null)
    {
        if (name is not null) Name = name;
        if (color is not null) Color = color;
        return Valid();
    }

    private DomainResult<Tag> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (string.IsNullOrWhiteSpace(Name)) errors.Add(DomainError.Create("Name is required."));
        return errors.Count > 0
            ? DomainResult<Tag>.Failure(errors)
            : DomainResult<Tag>.Success(this);
    }

    // Value-based equality: same TenantId + case-insensitive trimmed Name
    public bool Equals(Tag? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return TenantId == other.TenantId
            && string.Equals(Normalize(Name), Normalize(other.Name), StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Tag t && Equals(t);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            TenantId,
            (Normalize(Name) ?? string.Empty).ToUpperInvariant());
    }

    private static string? Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
