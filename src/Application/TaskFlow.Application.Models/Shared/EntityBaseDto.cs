using EF.Domain.Contracts;

namespace TaskFlow.Application.Models.Shared;

/// <summary>Carries entity base data across API, application, and UI boundaries.</summary>
public abstract record EntityBaseDto : IEntityBaseDto
{
    public Guid? Id { get; set; }
}
