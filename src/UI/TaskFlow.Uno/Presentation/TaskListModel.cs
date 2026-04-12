using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

public partial record TaskListModel(
    INavigator Navigator,
    ITaskItemApiService TaskItemService,
    ICategoryApiService CategoryService,
    IMessenger Messenger)
{
    public IListFeed<TaskItemModel> Items => ListFeed.Async(async ct =>
        (IImmutableList<TaskItemModel>)(await TaskItemService.SearchAsync(ct: ct)).ToImmutableList());

    public IState<string> SearchTerm => State<string>.Value(this, () => string.Empty);

    public IState<string> StatusFilter => State<string>.Value(this, () => string.Empty);

    public IState<string> PriorityFilter => State<string>.Value(this, () => string.Empty);

    public IListFeed<CategoryModel> Categories => ListFeed.Async(async ct =>
        (IImmutableList<CategoryModel>)(await CategoryService.SearchAsync(isActive: true, ct: ct)).ToImmutableList());

    public async ValueTask Search(CancellationToken ct)
    {
        var term = await SearchTerm;
        var status = await StatusFilter;
        var priority = await PriorityFilter;
        var results = await TaskItemService.SearchAsync(term, status, priority, ct: ct);
        // Results are surfaced through Items feed refresh
    }

    public async ValueTask OpenDetail(TaskItemModel item, CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskDetail", data: item, cancellation: ct);

    public async ValueTask CreateNew(CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskForm", cancellation: ct);

    public async ValueTask ToggleStatus(TaskItemModel item, CancellationToken ct)
    {
        var newStatus = item.Status switch
        {
            "Open" => "InProgress",
            "InProgress" => "Completed",
            _ => item.Status
        };

        await TaskItemService.UpdateAsync(item with { Status = newStatus }, ct);
    }
}
