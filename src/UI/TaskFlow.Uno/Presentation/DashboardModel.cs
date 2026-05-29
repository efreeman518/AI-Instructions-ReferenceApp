using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

/// <summary>Drives dashboard state, navigation, and commands for the Uno presentation layer.</summary>
public partial record DashboardModel
{
    /// <summary>Initializes dashboard model with required dependencies and default state.</summary>
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

    /// <summary>Refreshes refresh from the backing service.</summary>
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

    /// <summary>Navigates to navigate to task list using the configured navigator.</summary>
    public async ValueTask NavigateToTaskList(CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskList", cancellation: ct);

    /// <summary>Navigates to navigate to new task using the configured navigator.</summary>
    public async ValueTask NavigateToNewTask(CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskItem", cancellation: ct);

    /// <summary>Navigates to navigate to task using the configured navigator.</summary>
    public async ValueTask NavigateToTask(TaskItemModel task, CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskItem", data: task, cancellation: ct);
}
