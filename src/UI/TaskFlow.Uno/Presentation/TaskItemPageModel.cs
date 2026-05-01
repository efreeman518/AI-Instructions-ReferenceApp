using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

public partial record TaskItemPageModel
{
    public TaskItemModel? Entity { get; }
    private INavigator Navigator { get; }
    private ITaskItemApiService TaskItemService { get; }
    private ICategoryApiService CategoryService { get; }
    private ITagApiService TagService { get; }
    private ICommentApiService CommentService { get; }
    private IChecklistItemApiService ChecklistItemService { get; }
    private IAttachmentApiService AttachmentService { get; }
    private IMessenger Messenger { get; }
    private IFormGuard FormGuard { get; }

    // Mutable baseline so post-save equality reflects server-echoed state
    // without requiring us to reassign the init-only Entity property.
    private TaskItemModel _baseline;

    public TaskItemPageModel(
        TaskItemModel? entity,
        INavigator navigator,
        ITaskItemApiService taskItemService,
        ICategoryApiService categoryService,
        ITagApiService tagService,
        ICommentApiService commentService,
        IChecklistItemApiService checklistItemService,
        IAttachmentApiService attachmentService,
        IMessenger messenger,
        IFormGuard formGuard)
    {
        Entity = entity;
        Navigator = navigator;
        TaskItemService = taskItemService;
        CategoryService = categoryService;
        TagService = tagService;
        CommentService = commentService;
        ChecklistItemService = checklistItemService;
        AttachmentService = attachmentService;
        Messenger = messenger;
        FormGuard = formGuard;

        _baseline = entity ?? new TaskItemModel();
        FormGuard.IsDirtyAsync = ComputeIsDirtyAsync;

        Messenger.Register<TaskItemPageModel, TaskFormResetMessage>(this, static (recipient, msg) =>
        {
            _ = recipient.Reset();
        });
    }

    public IState<bool> IsEditMode => State<bool>.Value(this, () => Entity?.Id is not null);

    // ── Form fields ──────────────────────────────────────────────
    public IState<string> Title => State<string>.Value(this, () => Entity?.Title ?? string.Empty);
    public IState<string> Description => State<string>.Value(this, () => Entity?.Description ?? string.Empty);
    public IState<string> Priority => State<string>.Value(this, () => Entity?.Priority ?? "None");
    public IState<string> Status => State<string>.Value(this, () => Entity?.Status ?? "Open");
    public IState<DateTimeOffset?> StartDate => State<DateTimeOffset?>.Value(this, () => Entity?.StartDate);
    public IState<DateTimeOffset?> DueDate => State<DateTimeOffset?>.Value(this, () => Entity?.DueDate);
    public IState<Guid?> SelectedCategoryId => State<Guid?>.Value(this, () => Entity?.CategoryId);

    // ── Dynamic header text ──────────────────────────────────────
    public IState<string> FormHeader => State<string>.Value(this, () => Entity?.Id is not null ? "Edit Task" : "New Task");
    public IState<string> FormSubheader => State<string>.Value(this, () => Entity?.Id is not null ? "Update details, manage checklist and comments" : "Fill in the details to create a new task");
    public IState<string> SaveButtonText => State<string>.Value(this, () => Entity?.Id is not null ? "Update Task" : "Save Task");

    // ── Lookup lists ─────────────────────────────────────────────
    public IListFeed<CategoryModel> Categories => ListFeed.Async(async ct =>
        (IImmutableList<CategoryModel>)(await CategoryService.SearchAsync(isActive: true, ct: ct)).ToImmutableList());

    public IListFeed<TagModel> AvailableTags => ListFeed.Async(async ct =>
        (IImmutableList<TagModel>)(await TagService.SearchAsync(ct: ct)).ToImmutableList());

    // ── Children (mutable lists — add/delete updates immediately; initial
    //    load is via ListState.Async which fetches once then lets us mutate). ──
    public IListState<CommentModel> Comments => ListState.Async(this, async ct =>
    {
        if (Entity?.Id is null) return ImmutableList<CommentModel>.Empty;
        var result = await CommentService.SearchAsync(Entity.Id.Value, ct);
        return (IImmutableList<CommentModel>)result.ToImmutableList();
    });

    public IListState<ChecklistItemModel> ChecklistItems => ListState.Async(this, async ct =>
    {
        if (Entity?.Id is null) return ImmutableList<ChecklistItemModel>.Empty;
        var result = await ChecklistItemService.SearchAsync(Entity.Id.Value, ct: ct);
        return (IImmutableList<ChecklistItemModel>)result.ToImmutableList();
    });

    public IListState<AttachmentModel> Attachments => ListState.Async(this, async ct =>
    {
        if (Entity?.Id is null) return ImmutableList<AttachmentModel>.Empty;
        var result = await AttachmentService.SearchAsync(Entity.Id.Value, "TaskItem", ct);
        return (IImmutableList<AttachmentModel>)result.ToImmutableList();
    });

    // ── Inline add form states ───────────────────────────────────
    public IState<string> NewCommentBody => State<string>.Value(this, () => string.Empty);
    public IState<string> NewChecklistTitle => State<string>.Value(this, () => string.Empty);

    // ── Form reset (fired on page Visibility→Visible so create mode
    //    always starts empty, even when the ViewModel instance is reused
    //    by the Visibility navigator). ──
    public async ValueTask Reset(CancellationToken ct = default)
    {
        var noCt = CancellationToken.None;
        await Title.UpdateAsync(_ => Entity?.Title ?? string.Empty, noCt);
        await Description.UpdateAsync(_ => Entity?.Description ?? string.Empty, noCt);
        await Priority.UpdateAsync(_ => Entity?.Priority ?? "None", noCt);
        await Status.UpdateAsync(_ => Entity?.Status ?? "Open", noCt);
        await StartDate.UpdateAsync(_ => Entity?.StartDate, noCt);
        await DueDate.UpdateAsync(_ => Entity?.DueDate, noCt);
        await SelectedCategoryId.UpdateAsync(_ => Entity?.CategoryId, noCt);
        await NewCommentBody.UpdateAsync(_ => string.Empty, noCt);
        await NewChecklistTitle.UpdateAsync(_ => string.Empty, noCt);

        // Reload children from server in edit mode; clear them in create mode.
        if (Entity?.Id is Guid id)
        {
            var comments = await CommentService.SearchAsync(id, noCt);
            await Comments.UpdateAsync(_ => comments.ToImmutableList(), noCt);
            var checklist = await ChecklistItemService.SearchAsync(id, ct: noCt);
            await ChecklistItems.UpdateAsync(_ => checklist.ToImmutableList(), noCt);
            var attachments = await AttachmentService.SearchAsync(id, "TaskItem", noCt);
            await Attachments.UpdateAsync(_ => attachments.ToImmutableList(), noCt);
        }
        else
        {
            await Comments.UpdateAsync(_ => ImmutableList<CommentModel>.Empty, noCt);
            await ChecklistItems.UpdateAsync(_ => ImmutableList<ChecklistItemModel>.Empty, noCt);
            await Attachments.UpdateAsync(_ => ImmutableList<AttachmentModel>.Empty, noCt);
        }

        _baseline = Entity ?? new TaskItemModel();
        FormGuard.IsDirtyAsync = ComputeIsDirtyAsync;
    }

    // Called by the shell chrome before switching to a sibling route so
    // unsaved edits aren't silently discarded. Compares current field
    // state to the baseline snapshot taken on Reset/Save.
    private async ValueTask<bool> ComputeIsDirtyAsync(CancellationToken ct)
    {
        var title = (await Title) ?? string.Empty;
        var description = (await Description) ?? string.Empty;
        var priority = (await Priority) ?? "None";
        var status = (await Status) ?? "Open";
        var startDate = await StartDate;
        var dueDate = await DueDate;
        var categoryId = await SelectedCategoryId;
        var newComment = (await NewCommentBody) ?? string.Empty;
        var newChecklist = (await NewChecklistTitle) ?? string.Empty;

        var baseTitle = _baseline.Title ?? string.Empty;
        var baseDescription = _baseline.Description ?? string.Empty;
        var basePriority = _baseline.Priority ?? "None";
        var baseStatus = _baseline.Status ?? "Open";

        return title != baseTitle
            || description != baseDescription
            || priority != basePriority
            || status != baseStatus
            || startDate != _baseline.StartDate
            || dueDate != _baseline.DueDate
            || categoryId != _baseline.CategoryId
            || !string.IsNullOrWhiteSpace(newComment)
            || !string.IsNullOrWhiteSpace(newChecklist);
    }

    // ── Save (create or update) ──────────────────────────────────
    public async ValueTask Save(CancellationToken ct)
    {
        var title = await Title;
        var description = await Description;
        var priority = await Priority;
        var status = await Status;
        var startDate = await StartDate;
        var dueDate = await DueDate;
        var categoryId = await SelectedCategoryId;

        if (string.IsNullOrWhiteSpace(title)) return;

        // Gather any buffered children so the task + its children ship in a
        // single request. Buffered children carry a client-side Id for local
        // tracking — strip it so the server assigns a real Id.
        var pendingChecklist = (await ChecklistItems) ?? ImmutableList<ChecklistItemModel>.Empty;
        var pendingComments = (await Comments) ?? ImmutableList<CommentModel>.Empty;
        var isCreate = Entity?.Id is null;

        var model = (Entity ?? new TaskItemModel()) with
        {
            Title = title,
            Description = description,
            Priority = priority ?? "None",
            Status = status ?? "Open",
            StartDate = startDate,
            DueDate = dueDate,
            CategoryId = categoryId,
            ChecklistItems = isCreate
                ? pendingChecklist.Select(c => c with { Id = null, TaskItemId = Guid.Empty }).ToList()
                : pendingChecklist.ToList(),
            Comments = isCreate
                ? pendingComments.Select(c => c with { Id = null, TaskItemId = Guid.Empty }).ToList()
                : pendingComments.ToList()
        };

        var wasCreate = !model.Id.HasValue;
        var saved = wasCreate
            ? await TaskItemService.CreateAsync(model, ct)
            : await TaskItemService.UpdateAsync(model, ct);

        _baseline = saved ?? model;
        FormGuard.Clear();

        Messenger.Send(new TaskItemsChangedMessage(ResetToFirstPage: wasCreate));

        if (wasCreate)
        {
            await Navigator.NavigateRouteAsync(this, "/Main/TaskList", cancellation: CancellationToken.None);
        }
        else
        {
            await Navigator.NavigateBackAsync(this, cancellation: CancellationToken.None);
        }
    }

    // ── Delete ───────────────────────────────────────────────────
    public async ValueTask DeleteTask(CancellationToken ct)
    {
        if (Entity?.Id is null) return;
        await TaskItemService.DeleteAsync(Entity.Id.Value, ct);
        FormGuard.Clear();
        Messenger.Send(new TaskItemsChangedMessage(ResetToFirstPage: true));
        await Navigator.NavigateBackAsync(this, cancellation: CancellationToken.None);
    }

    // ── Comment commands (buffered in create mode; persisted on task Save) ──
    public async ValueTask AddComment(CancellationToken ct)
    {
        var body = await NewCommentBody;
        if (string.IsNullOrWhiteSpace(body)) return;

        CommentModel created;
        if (Entity?.Id is Guid taskId)
        {
            created = await CommentService.CreateAsync(new CommentModel { Body = body, TaskItemId = taskId }, ct);
        }
        else
        {
            // Buffered comment: assign a client-side Id for local tracking.
            // It's stripped before the server save.
            created = new CommentModel { Id = Guid.NewGuid(), Body = body };
        }

        await NewCommentBody.UpdateAsync(_ => string.Empty, CancellationToken.None);
        await Comments.UpdateAsync(list => (list ?? ImmutableList<CommentModel>.Empty).Add(created), CancellationToken.None);
    }

    public async ValueTask DeleteComment(CommentModel comment, CancellationToken ct)
    {
        if (Entity?.Id is not null && comment.Id is Guid id)
        {
            await CommentService.DeleteAsync(id, ct);
            await Comments.UpdateAsync(list => (list ?? ImmutableList<CommentModel>.Empty).RemoveAll(c => c.Id == id), CancellationToken.None);
        }
        else if (comment.Id is Guid clientId)
        {
            await Comments.UpdateAsync(list => (list ?? ImmutableList<CommentModel>.Empty).RemoveAll(c => c.Id == clientId), CancellationToken.None);
        }
        else
        {
            await Comments.UpdateAsync(list => (list ?? ImmutableList<CommentModel>.Empty).Remove(comment), CancellationToken.None);
        }
    }

    // ── Checklist commands (buffered in create mode; persisted on task Save) ──
    public async ValueTask AddChecklistItem(CancellationToken ct)
    {
        var title = await NewChecklistTitle;
        if (string.IsNullOrWhiteSpace(title)) return;

        ChecklistItemModel created;
        if (Entity?.Id is Guid taskId)
        {
            created = await ChecklistItemService.CreateAsync(new ChecklistItemModel { Title = title, TaskItemId = taskId }, ct);
        }
        else
        {
            // Buffered item: assign a client-side Id so subsequent toggle/delete
            // operations can find it reliably by Id. The Id will be sent to the
            // server when the parent task is saved.
            created = new ChecklistItemModel { Id = Guid.NewGuid(), Title = title };
        }

        await NewChecklistTitle.UpdateAsync(_ => string.Empty, CancellationToken.None);
        await ChecklistItems.UpdateAsync(list => (list ?? ImmutableList<ChecklistItemModel>.Empty).Add(created), CancellationToken.None);
    }

    public async ValueTask ToggleChecklistItem(ChecklistItemModel item, CancellationToken ct)
    {
        var updated = item with { IsCompleted = !item.IsCompleted };

        // Update the in-memory list first so the UI reflects the new state
        // immediately. If the parent task is persisted, send the change to
        // the server afterwards; in create mode the item gets persisted
        // with its current IsCompleted when the parent task is saved.
        await ChecklistItems.UpdateAsync((IImmutableList<ChecklistItemModel> list) =>
        {
            var source = list ?? (IImmutableList<ChecklistItemModel>)ImmutableList<ChecklistItemModel>.Empty;
            var idx = FindIndex(source, item);
            return idx < 0 ? source : source.SetItem(idx, updated);
        }, CancellationToken.None);

        if (Entity?.Id is not null && item.Id is Guid)
        {
            try { await ChecklistItemService.UpdateAsync(updated, ct); }
            catch (Exception ex) { Console.WriteLine($"[Toggle] server update failed: {ex.Message}"); }
        }

        static int FindIndex(IImmutableList<ChecklistItemModel> items, ChecklistItemModel target)
        {
            for (var i = 0; i < items.Count; i++)
            {
                // Match by Id first (both buffered and saved items have one);
                // fall back to reference/value equality.
                if (target.Id is Guid id && items[i].Id == id) return i;
                if (ReferenceEquals(items[i], target)) return i;
            }
            return -1;
        }
    }

    public async ValueTask DeleteChecklistItem(ChecklistItemModel item, CancellationToken ct)
    {
        // Server call only when the parent task is persisted AND this item has an Id
        // AND the Id is not a client-side buffered one (we can't reliably tell by Id,
        // so use Entity.Id presence as the persisted-task gate).
        if (Entity?.Id is not null && item.Id is Guid id)
        {
            await ChecklistItemService.DeleteAsync(id, ct);
            await ChecklistItems.UpdateAsync(list => (list ?? ImmutableList<ChecklistItemModel>.Empty).RemoveAll(c => c.Id == id), CancellationToken.None);
        }
        else if (item.Id is Guid clientId)
        {
            await ChecklistItems.UpdateAsync(list => (list ?? ImmutableList<ChecklistItemModel>.Empty).RemoveAll(c => c.Id == clientId), CancellationToken.None);
        }
        else
        {
            await ChecklistItems.UpdateAsync(list => (list ?? ImmutableList<ChecklistItemModel>.Empty).Remove(item), CancellationToken.None);
        }
    }

    // ── Attachment commands ──────────────────────────────────────
    public async ValueTask DeleteAttachment(AttachmentModel attachment, CancellationToken ct)
    {
        if (attachment.Id is null) return;
        await AttachmentService.DeleteAsync(attachment.Id.Value, ct);
        await Attachments.UpdateAsync(list => (list ?? ImmutableList<AttachmentModel>.Empty).RemoveAll(a => a.Id == attachment.Id), ct);
    }
}
