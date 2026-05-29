namespace TaskFlow.Application.Models.Shared;

/// <summary>Carries i entity base data across API, application, and UI boundaries.</summary>
public interface IEntityBaseDto
{
    Guid? Id { get; set; }
}
