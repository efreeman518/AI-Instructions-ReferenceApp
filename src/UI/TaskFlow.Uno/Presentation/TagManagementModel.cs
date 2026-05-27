using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

namespace TaskFlow.Uno.Presentation;

public partial record TagManagementModel(
    INavigator Navigator,
    ITagApiService TagService,
    IMessenger Messenger)
{
    private TagModel? _editingTag;

    public IState<int> TagsVersion => State<int>.Value(this, () => 0);

    public IListFeed<TagModel> Tags => ListFeed.Async(async ct =>
    {
        _ = await TagsVersion;
        return (IImmutableList<TagModel>)(await TagService.SearchAsync(ct: ct)).ToImmutableList();
    });

    public IState<string> NewTagName => State<string>.Value(this, () => string.Empty);
    public IState<string> NewTagColor => State<string>.Value(this, () => "#3B82F6");

    public async ValueTask CreateTag(CancellationToken ct)
    {
        var name = await NewTagName;
        var color = await NewTagColor;
        if (string.IsNullOrWhiteSpace(name)) return;

        await TagService.CreateAsync(new TagModel { Name = name, Color = color }, ct);
        await NewTagName.UpdateAsync(_ => string.Empty, ct);
        await TagsVersion.UpdateAsync(version => version + 1, ct);
    }

    public async ValueTask UpdateTag(CancellationToken ct)
    {
        var editing = _editingTag;
        if (editing?.Id is null) return;

        await TagService.UpdateAsync(editing, ct);
        _editingTag = null;
        await TagsVersion.UpdateAsync(version => version + 1, ct);
    }

    public async ValueTask DeleteTag(TagModel tag, CancellationToken ct)
    {
        if (tag.Id is null) return;
        await TagService.DeleteAsync(tag.Id.Value, ct);
        await TagsVersion.UpdateAsync(version => version + 1, ct);
    }

    public ValueTask StartEdit(TagModel tag, CancellationToken ct)
    {
        _editingTag = tag;
        return ValueTask.CompletedTask;
    }

    public ValueTask CancelEdit(CancellationToken ct)
    {
        _editingTag = null;
        return ValueTask.CompletedTask;
    }
}
