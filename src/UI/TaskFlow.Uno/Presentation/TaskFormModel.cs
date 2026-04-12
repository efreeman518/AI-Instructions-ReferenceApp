using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

public partial record TaskFormModel(
    INavigator Navigator,
    ITaskItemApiService TaskItemService,
    ICategoryApiService CategoryService,
    ITagApiService TagService,
    IMessenger Messenger)
{
    public IState<TaskItemModel> Task => State<TaskItemModel>.Value(this, () => new TaskItemModel());

    public IListFeed<CategoryModel> Categories => ListFeed.Async(async ct =>
        (IImmutableList<CategoryModel>)(await CategoryService.SearchAsync(isActive: true, ct: ct)).ToImmutableList());

    public IListFeed<TagModel> AvailableTags => ListFeed.Async(async ct =>
        (IImmutableList<TagModel>)(await TagService.SearchAsync(ct: ct)).ToImmutableList());

    public IState<string> Title => State<string>.Value(this, () => string.Empty);
    public IState<string> Description => State<string>.Value(this, () => string.Empty);
    public IState<string> Priority => State<string>.Value(this, () => "None");
    public IState<DateTimeOffset?> StartDate => State<DateTimeOffset?>.Value(this, () => null);
    public IState<DateTimeOffset?> DueDate => State<DateTimeOffset?>.Value(this, () => null);
    public IState<Guid?> SelectedCategoryId => State<Guid?>.Value(this, () => null);

    public async ValueTask Save(CancellationToken ct)
    {
        var task = await Task;
        var title = await Title;
        var description = await Description;
        var priority = await Priority;
        var startDate = await StartDate;
        var dueDate = await DueDate;
        var categoryId = await SelectedCategoryId;

        if (string.IsNullOrWhiteSpace(title)) return;

        var model = (task ?? new TaskItemModel()) with
        {
            Title = title,
            Description = description,
            Priority = priority ?? "None",
            StartDate = startDate,
            DueDate = dueDate,
            CategoryId = categoryId
        };

        if (model.Id.HasValue)
        {
            await TaskItemService.UpdateAsync(model, ct);
        }
        else
        {
            await TaskItemService.CreateAsync(model, ct);
        }

        await Navigator.NavigateBackAsync(this, cancellation: ct);
    }

    public async ValueTask Cancel(CancellationToken ct) =>
        await Navigator.NavigateBackAsync(this, cancellation: ct);
}
