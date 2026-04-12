using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

public partial record CategoryTreeModel(
    INavigator Navigator,
    ICategoryApiService CategoryService,
    IMessenger Messenger)
{
    public IListFeed<CategoryModel> Categories => ListFeed.Async(async ct =>
    {
        var all = await CategoryService.SearchAsync(ct: ct);
        return (IImmutableList<CategoryModel>)BuildTree(all).ToImmutableList();
    });

    public IState<string> NewCategoryName => State<string>.Value(this, () => string.Empty);
    public IState<string> NewCategoryDescription => State<string>.Value(this, () => string.Empty);
    public IState<Guid?> SelectedParentId => State<Guid?>.Value(this, () => null);
    public IState<CategoryModel?> EditingCategory => State<CategoryModel?>.Value(this, () => null);

    public async ValueTask CreateCategory(CancellationToken ct)
    {
        var name = await NewCategoryName;
        var description = await NewCategoryDescription;
        var parentId = await SelectedParentId;

        if (string.IsNullOrWhiteSpace(name)) return;

        await CategoryService.CreateAsync(new CategoryModel
        {
            Name = name, Description = description, ParentCategoryId = parentId, IsActive = true
        }, ct);

        await NewCategoryName.UpdateAsync(_ => string.Empty, ct);
        await NewCategoryDescription.UpdateAsync(_ => string.Empty, ct);
    }

    public async ValueTask UpdateCategory(CancellationToken ct)
    {
        var editing = await EditingCategory;
        if (editing?.Id is null) return;

        await CategoryService.UpdateAsync(editing, ct);
        await EditingCategory.UpdateAsync(_ => null, ct);
    }

    public async ValueTask DeleteCategory(CategoryModel category, CancellationToken ct)
    {
        if (category.Id is null) return;
        await CategoryService.DeleteAsync(category.Id.Value, ct);
    }

    private static IReadOnlyList<CategoryModel> BuildTree(IReadOnlyList<CategoryModel> flat)
    {
        var lookup = flat.ToLookup(c => c.ParentCategoryId);
        return BuildChildren(null, lookup);
    }

    private static IReadOnlyList<CategoryModel> BuildChildren(Guid? parentId,
        ILookup<Guid?, CategoryModel> lookup)
    {
        return lookup[parentId]
            .Select(c => c with { Children = BuildChildren(c.Id, lookup) })
            .OrderBy(c => c.SortOrder)
            .ToList();
    }
}
