using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

public partial record DashboardModel
{
    public DashboardModel(
        INavigator navigator,
        IDashboardService dashboardService,
        IMessenger messenger)
    {
        Navigator = navigator;
        DashboardService = dashboardService;
        Messenger = messenger;

        Messenger.Register<DashboardModel, TaskItemsChangedMessage>(this, static (recipient, msg) =>
        {
            _ = recipient.RefreshAsync();
        });

        _ = RefreshAsync();
    }

    private INavigator Navigator { get; }

    private IDashboardService DashboardService { get; }

    private IMessenger Messenger { get; }

    public IState<DashboardSummary> Summary => State<DashboardSummary>.Value(this, () => new DashboardSummary());
    public IState<bool> IsLoading => State<bool>.Value(this, () => false);

    public async ValueTask RefreshAsync(CancellationToken ct = default)
    {
        await IsLoading.UpdateAsync(_ => true, ct);
        try
        {
            var result = await DashboardService.GetSummaryAsync(ct);
            await Summary.UpdateAsync(_ => result, ct);
        }
        finally
        {
            await IsLoading.UpdateAsync(_ => false, ct);
        }
    }

    public async ValueTask NavigateToTaskList(CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskList", cancellation: ct);

    public async ValueTask NavigateToNewTask(CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskItem", cancellation: ct);

    public async ValueTask NavigateToTask(TaskItemModel task, CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskItem", data: task, cancellation: ct);
}
