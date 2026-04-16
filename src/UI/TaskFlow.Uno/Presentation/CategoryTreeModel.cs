using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

#pragma warning disable CS8620, CS8714 // Uno MVUX IState<T?>/IFeed<T> nullability mismatch

namespace TaskFlow.Uno.Presentation;

public partial record CategoryTreeModel(
    INavigator Navigator,
    ICategoryApiService CategoryService,
    IMessenger Messenger)
{
    public IState<int> CategoriesVersion => State<int>.Value(this, () => 0);
    public IState<bool> IsEditing => State<bool>.Value(this, () => false);
    public IState<bool> IsCreating => State<bool>.Value(this, () => true);

    public IListFeed<CategoryModel> Categories => ListFeed.Async(async ct =>
    {
        _ = await CategoriesVersion;
        return (IImmutableList<CategoryModel>)(await CategoryService.SearchAsync(ct: ct))
            .OrderBy(category => category.ParentCategoryId.HasValue ? 1 : 0)
            .ThenBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToImmutableList();
    });

    public IState<string> NewCategoryName => State<string>.Value(this, () => string.Empty);
    public IState<string> NewCategoryDescription => State<string>.Value(this, () => string.Empty);
    public IState<Guid?> SelectedParentId => State<Guid?>.Value(this, () => null);
    public IState<CategoryModel?> EditingCategory => State<CategoryModel?>.Value(this, () => null);

    public async ValueTask SaveCategory(CancellationToken ct)
    {
        var name = await NewCategoryName;
        var description = await NewCategoryDescription;
        var parentId = await SelectedParentId;
        var editing = await EditingCategory;

        if (string.IsNullOrWhiteSpace(name)) return;

        var category = (editing ?? new CategoryModel()) with
        {
            Name = name,
            Description = description,
            ParentCategoryId = parentId,
            IsActive = editing?.IsActive ?? true,
            SortOrder = editing?.SortOrder ?? 0
        };

        if (editing?.Id is not null)
        {
            await CategoryService.UpdateAsync(category, ct);
        }
        else
        {
            await CategoryService.CreateAsync(category, ct);
        }

        await ResetEditor(ct);
        await CategoriesVersion.UpdateAsync(version => version + 1, ct);
    }

    public async ValueTask StartEdit(CategoryModel category, CancellationToken ct)
    {
        await EditingCategory.UpdateAsync(_ => category, ct);
        await NewCategoryName.UpdateAsync(_ => category.Name, ct);
        await NewCategoryDescription.UpdateAsync(_ => category.Description ?? string.Empty, ct);
        await SelectedParentId.UpdateAsync(_ => category.ParentCategoryId, ct);
        await IsEditing.UpdateAsync(_ => true, ct);
        await IsCreating.UpdateAsync(_ => false, ct);
    }

    public async ValueTask CancelEdit(CancellationToken ct) => await ResetEditor(ct);

    public async ValueTask DeleteCategory(CategoryModel category, CancellationToken ct)
    {
        if (category.Id is null) return;
        await CategoryService.DeleteAsync(category.Id.Value, ct);
        await CategoriesVersion.UpdateAsync(version => version + 1, ct);
    }

    private async ValueTask ResetEditor(CancellationToken ct)
    {
        await EditingCategory.UpdateAsync(_ => null, ct);
        await NewCategoryName.UpdateAsync(_ => string.Empty, ct);
        await NewCategoryDescription.UpdateAsync(_ => string.Empty, ct);
        await SelectedParentId.UpdateAsync(_ => null, ct);
        await IsEditing.UpdateAsync(_ => false, ct);
        await IsCreating.UpdateAsync(_ => true, ct);
    }
}
