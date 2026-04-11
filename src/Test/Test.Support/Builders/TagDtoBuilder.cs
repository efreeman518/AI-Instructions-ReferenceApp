using TaskFlow.Application.Models;

namespace Test.Support.Builders;

public class TagDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _name = "Test Tag";
    private string? _color = "#FF0000";

    public TagDtoBuilder WithId(Guid? id) { _id = id; return this; }
    public TagDtoBuilder WithName(string name) { _name = name; return this; }
    public TagDtoBuilder WithColor(string? color) { _color = color; return this; }

    public TagDto Build() => new()
    {
        Id = _id,
        Name = _name,
        Color = _color
    };
}
