using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

public partial record DashboardModel(
    INavigator Navigator,
    IDashboardService DashboardService,
    IMessenger Messenger)
{
    public IFeed<DashboardSummary> Summary => Feed.Async(async ct => await DashboardService.GetSummaryAsync(ct));

    public async ValueTask NavigateToTaskList(CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskList", cancellation: ct);

    public async ValueTask NavigateToNewTask(CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskForm", cancellation: ct);

    public async ValueTask NavigateToTask(TaskItemModel task, CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskDetail", data: task, cancellation: ct);
}
