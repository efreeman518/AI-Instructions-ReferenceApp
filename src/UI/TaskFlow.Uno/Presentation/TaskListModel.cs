using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

public partial record TaskListModel
{
    private const int DefaultPageSize = 10;

    public TaskListModel(
        INavigator navigator,
        ITaskItemApiService taskItemService,
        ICategoryApiService categoryService,
        IMessenger messenger)
    {
        Navigator = navigator;
        TaskItemService = taskItemService;
        CategoryService = categoryService;
        Messenger = messenger;

        Messenger.Register<TaskListModel, TaskItemsChangedMessage>(this, static (recipient, message) =>
        {
            if (message.ResetToFirstPage)
            {
                _ = recipient.LoadPageAsync(1);
            }
            else
            {
                _ = recipient.RefreshAsync();
            }
        });

        _ = LoadPageAsync(1);
    }

    private INavigator Navigator { get; }
    private ITaskItemApiService TaskItemService { get; }
    private ICategoryApiService CategoryService { get; }
    private IMessenger Messenger { get; }

    public IImmutableList<int> PageSizeOptions { get; } = [10, 20, 50];

    // ── List + page-metadata state (individually bindable) ──
    public IListState<TaskItemModel> Items => ListState<TaskItemModel>.Empty(this);
    public IState<int> PageNumber => State<int>.Value(this, () => 1);
    public IState<int> TotalCount => State<int>.Value(this, () => 0);
    public IState<int> TotalPages => State<int>.Value(this, () => 1);
    public IState<int> StartItemNumber => State<int>.Value(this, () => 0);
    public IState<int> EndItemNumber => State<int>.Value(this, () => 0);
    public IState<bool> HasPreviousPage => State<bool>.Value(this, () => false);
    public IState<bool> HasNextPage => State<bool>.Value(this, () => false);
    public IState<bool> ShowPager => State<bool>.Value(this, () => false);
    public IState<bool> HasItems => State<bool>.Value(this, () => false);
    public IState<bool> IsEmpty => State<bool>.Value(this, () => true);
    public IListState<int> VisiblePageNumbers => ListState<int>.Empty(this);

    // ── User-input state ──
    public IState<int> CurrentPage => State<int>.Value(this, () => 1);
    public IState<int> SelectedPageSize => State<int>.Value(this, () => DefaultPageSize);
    public IState<int> PageSize => State<int>.Value(this, () => DefaultPageSize);
    public IState<string> SearchTerm => State<string>.Value(this, () => string.Empty);
    public IState<string> AppliedSearchTerm => State<string>.Value(this, () => string.Empty);
    public IState<string> StatusFilter => State<string>.Value(this, () => string.Empty);
    public IState<string> PriorityFilter => State<string>.Value(this, () => string.Empty);

    public IListFeed<CategoryModel> Categories => ListFeed.Async(async ct =>
        (IImmutableList<CategoryModel>)(await CategoryService.SearchAsync(isActive: true, ct: ct)).ToImmutableList());

    public async ValueTask RefreshAsync(CancellationToken ct = default)
    {
        var page = await CurrentPage;
        await LoadPageAsync(page, ct);
    }

    public async ValueTask LoadPageAsync(int targetPage, CancellationToken ct = default)
    {
        var page = Math.Max(1, targetPage);
        var size = await PageSize;
        var term = await AppliedSearchTerm;
        var status = await StatusFilter;
        var priority = await PriorityFilter;

        System.Diagnostics.Debug.WriteLine($"[TaskList] LoadPageAsync requested page={page} size={size}");
        Console.WriteLine($"[TaskList] LoadPageAsync requested page={page} size={size}");

        // CancellationToken.None for the fetch so MVUX command cancellation
        // (which fires when IsEnabled bindings flip during state updates)
        // can't abort mid-request.
        var result = await TaskItemService.SearchPageAsync(
            searchTerm: term,
            status: status,
            priority: priority,
            pageNumber: page,
            pageSize: size,
            ct: CancellationToken.None);

        System.Diagnostics.Debug.WriteLine($"[TaskList] Got result page={result.PageNumber} total={result.TotalCount} items={result.Items.Count}");
        Console.WriteLine($"[TaskList] Got result page={result.PageNumber} total={result.TotalCount} items={result.Items.Count}");

        // Update every bindable field explicitly. CancellationToken.None so
        // state writes aren't aborted by command CT cancellation.
        var noCt = CancellationToken.None;
        await Items.UpdateAsync(_ => result.Items.ToImmutableList(), noCt);
        await CurrentPage.UpdateAsync(_ => result.PageNumber, noCt);
        await PageNumber.UpdateAsync(_ => result.PageNumber, noCt);
        await TotalCount.UpdateAsync(_ => result.TotalCount, noCt);
        await TotalPages.UpdateAsync(_ => result.TotalPages, noCt);
        await StartItemNumber.UpdateAsync(_ => result.StartItemNumber, noCt);
        await EndItemNumber.UpdateAsync(_ => result.EndItemNumber, noCt);
        await HasPreviousPage.UpdateAsync(_ => result.HasPreviousPage, noCt);
        await HasNextPage.UpdateAsync(_ => result.HasNextPage, noCt);
        await ShowPager.UpdateAsync(_ => result.ShowPager, noCt);
        await HasItems.UpdateAsync(_ => result.HasItems, noCt);
        await IsEmpty.UpdateAsync(_ => result.IsEmpty, noCt);
        await VisiblePageNumbers.UpdateAsync(_ => result.VisiblePageNumbers.ToImmutableList(), noCt);
    }

    public async ValueTask Search(CancellationToken ct)
    {
        var term = (await SearchTerm) ?? string.Empty;
        var selectedSize = await SelectedPageSize;

        await AppliedSearchTerm.UpdateAsync(_ => term.Trim(), ct);
        await PageSize.UpdateAsync(_ => NormalizePageSize(selectedSize), ct);
        await LoadPageAsync(1, ct);
    }

    public async ValueTask OpenDetail(TaskItemModel item, CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskItem", data: item, cancellation: ct);

    public async ValueTask CreateNew(CancellationToken ct) =>
        await Navigator.NavigateRouteAsync(this, "TaskItem", cancellation: ct);

    public async ValueTask ToggleStatus(TaskItemModel item, CancellationToken ct)
    {
        var newStatus = item.Status switch
        {
            "Open" => "InProgress",
            "InProgress" => "Completed",
            _ => item.Status
        };

        await TaskItemService.UpdateAsync(item with { Status = newStatus }, ct);
        await RefreshAsync(ct);
        Messenger.Send(new TaskItemsChangedMessage());
    }

    public async ValueTask PreviousPage(CancellationToken ct)
    {
        var current = await CurrentPage;
        if (current <= 1) return;
        await LoadPageAsync(current - 1, ct);
    }

    public async ValueTask NextPage(CancellationToken ct)
    {
        var current = await CurrentPage;
        var total = await TotalPages;
        if (current >= total) return;
        await LoadPageAsync(current + 1, ct);
    }

    public async ValueTask FirstPage(CancellationToken ct) =>
        await LoadPageAsync(1, ct);

    public async ValueTask LastPage(CancellationToken ct)
    {
        var total = await TotalPages;
        await LoadPageAsync(total, ct);
    }

    public async ValueTask GoToPage(int pageNumber, CancellationToken ct) =>
        await LoadPageAsync(pageNumber, ct);

    private static int NormalizePageSize(int pageSize) => pageSize is 10 or 20 or 50 ? pageSize : DefaultPageSize;
}
