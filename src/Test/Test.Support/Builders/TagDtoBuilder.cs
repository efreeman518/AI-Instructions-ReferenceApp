using TaskFlow.Application.Models;

namespace Test.Support.Builders;

/// <summary>Builds tag DTO test data with sensible defaults so tests only override relevant fields.</summary>
public class TagDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _name = "Test Tag";
    private string? _color = "#FF0000";

    /// <summary>Sets ID on the builder so tests can override only scenario-specific values.</summary>
    public TagDtoBuilder WithId(Guid? id) { _id = id; return this; }
    /// <summary>Sets name on the builder so tests can override only scenario-specific values.</summary>
    public TagDtoBuilder WithName(string name) { _name = name; return this; }
    /// <summary>Sets color on the builder so tests can override only scenario-specific values.</summary>
    public TagDtoBuilder WithColor(string? color) { _color = color; return this; }

    /// <summary>Builds test data used by focused test cases.</summary>
    public TagDto Build() => new()
    {
        Id = _id,
        Name = _name,
        Color = _color
    };
}
