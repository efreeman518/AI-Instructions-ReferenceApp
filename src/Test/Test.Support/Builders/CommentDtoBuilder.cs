using TaskFlow.Application.Models;

namespace Test.Support.Builders;

public class CommentDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _body = "Test comment";
    private Guid _taskItemId = Guid.NewGuid();

    public CommentDtoBuilder WithId(Guid? id) { _id = id; return this; }
    public CommentDtoBuilder WithBody(string body) { _body = body; return this; }
    public CommentDtoBuilder WithTaskItemId(Guid taskItemId) { _taskItemId = taskItemId; return this; }

    public CommentDto Build() => new()
    {
        Id = _id,
        Body = _body,
        TaskItemId = _taskItemId
    };
}
