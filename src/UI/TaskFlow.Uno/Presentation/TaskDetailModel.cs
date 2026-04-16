using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

public partial record TaskDetailModel(
    INavigator Navigator,
    ITaskItemApiService TaskItemService,
    ICommentApiService CommentService,
    IChecklistItemApiService ChecklistItemService,
    IAttachmentApiService AttachmentService,
    IMessenger Messenger)
{
    public IState<TaskItemModel> Task => State<TaskItemModel>.Empty(this);

    public IListFeed<CommentModel> Comments => ListFeed.Async(async ct =>
    {
        var task = await Task;
        if (task?.Id is null) return ImmutableList<CommentModel>.Empty;
        return (IImmutableList<CommentModel>)(await CommentService.SearchAsync(task.Id, ct)).ToImmutableList();
    });

    public IListFeed<ChecklistItemModel> ChecklistItems => ListFeed.Async(async ct =>
    {
        var task = await Task;
        if (task?.Id is null) return ImmutableList<ChecklistItemModel>.Empty;
        return (IImmutableList<ChecklistItemModel>)(await ChecklistItemService.SearchAsync(task.Id, ct: ct)).ToImmutableList();
    });

    public IListFeed<AttachmentModel> Attachments => ListFeed.Async(async ct =>
    {
        var task = await Task;
        if (task?.Id is null) return ImmutableList<AttachmentModel>.Empty;
        return (IImmutableList<AttachmentModel>)(await AttachmentService.SearchAsync(task.Id, "TaskItem", ct)).ToImmutableList();
    });

    public IListFeed<TaskItemModel> SubTasks => ListFeed.Async(async ct =>
    {
        var task = await Task;
        if (task?.Id is null) return ImmutableList<TaskItemModel>.Empty;
        return (IImmutableList<TaskItemModel>)(await TaskItemService.SearchAsync(ct: ct))
            .Where(t => t.ParentTaskItemId == task.Id).ToImmutableList();
    });

    public IState<string> NewCommentBody => State<string>.Value(this, () => string.Empty);

    public IState<string> NewChecklistTitle => State<string>.Value(this, () => string.Empty);

    public async ValueTask AddComment(CancellationToken ct)
    {
        var task = await Task;
        var body = await NewCommentBody;
        if (task?.Id is null || string.IsNullOrWhiteSpace(body)) return;

        await CommentService.CreateAsync(new CommentModel { Body = body, TaskItemId = task.Id.Value }, ct);
        await NewCommentBody.UpdateAsync(_ => string.Empty, ct);
    }

    public async ValueTask DeleteComment(CommentModel comment, CancellationToken ct)
    {
        if (comment.Id is null) return;
        await CommentService.DeleteAsync(comment.Id.Value, ct);
    }

    public async ValueTask AddChecklistItem(CancellationToken ct)
    {
        var task = await Task;
        var title = await NewChecklistTitle;
        if (task?.Id is null || string.IsNullOrWhiteSpace(title)) return;

        await ChecklistItemService.CreateAsync(new ChecklistItemModel { Title = title, TaskItemId = task.Id.Value }, ct);
        await NewChecklistTitle.UpdateAsync(_ => string.Empty, ct);
    }

    public async ValueTask ToggleChecklistItem(ChecklistItemModel item, CancellationToken ct)
    {
        if (item.Id is null) return;
        await ChecklistItemService.UpdateAsync(item with { IsCompleted = !item.IsCompleted }, ct);
    }

    public async ValueTask DeleteChecklistItem(ChecklistItemModel item, CancellationToken ct)
    {
        if (item.Id is null) return;
        await ChecklistItemService.DeleteAsync(item.Id.Value, ct);
    }

    public async ValueTask DeleteAttachment(AttachmentModel attachment, CancellationToken ct)
    {
        if (attachment.Id is null) return;
        await AttachmentService.DeleteAsync(attachment.Id.Value, ct);
    }

    public async ValueTask EditTask(CancellationToken ct)
    {
        var task = await Task;
        if (task is null) return;
        await Navigator.NavigateRouteAsync(this, "TaskForm", data: task, cancellation: ct);
    }

    public async ValueTask DeleteTask(CancellationToken ct)
    {
        var task = await Task;
        if (task?.Id is null) return;
        await TaskItemService.DeleteAsync(task.Id.Value, ct);
        await Navigator.NavigateRouteAsync(this, "TaskList", cancellation: ct);
    }
}
