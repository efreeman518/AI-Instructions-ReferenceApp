using TaskFlow.Application.Models;

namespace Test.Support.Builders;

/// <summary>Builds comment DTO test data with sensible defaults so tests only override relevant fields.</summary>
public class CommentDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _body = "Test comment";
    private Guid _taskItemId = Guid.NewGuid();

    /// <summary>Sets ID on the builder so tests can override only scenario-specific values.</summary>
    public CommentDtoBuilder WithId(Guid? id) { _id = id; return this; }
    /// <summary>Sets body on the builder so tests can override only scenario-specific values.</summary>
    public CommentDtoBuilder WithBody(string body) { _body = body; return this; }
    /// <summary>Sets task item ID on the builder so tests can override only scenario-specific values.</summary>
    public CommentDtoBuilder WithTaskItemId(Guid taskItemId) { _taskItemId = taskItemId; return this; }

    /// <summary>Builds test data used by focused test cases.</summary>
    public CommentDto Build() => new()
    {
        Id = _id,
        Body = _body,
        TaskItemId = _taskItemId
    };
}
