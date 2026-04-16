namespace TaskFlow.Application.Models.Shared;

public abstract record EntityBaseDto : IEntityBaseDto
{
    public Guid? Id { get; set; }
}
