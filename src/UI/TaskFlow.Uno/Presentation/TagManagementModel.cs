using CommunityToolkit.Mvvm.Messaging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;

#pragma warning disable CS8620, CS8714 // Uno MVUX IState<T?>/IFeed<T> nullability mismatch

namespace TaskFlow.Uno.Presentation;

public partial record TagManagementModel(
    INavigator Navigator,
    ITagApiService TagService,
    IMessenger Messenger)
{
    public IState<int> TagsVersion => State<int>.Value(this, () => 0);

    public IListFeed<TagModel> Tags => ListFeed.Async(async ct =>
    {
        _ = await TagsVersion;
        return (IImmutableList<TagModel>)(await TagService.SearchAsync(ct: ct)).ToImmutableList();
    });

    public IState<string> NewTagName => State<string>.Value(this, () => string.Empty);
    public IState<string> NewTagColor => State<string>.Value(this, () => "#3B82F6");
    public IState<TagModel?> EditingTag => State<TagModel?>.Value(this, () => null);

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
        var editing = await EditingTag;
        if (editing?.Id is null) return;

        await TagService.UpdateAsync(editing, ct);
        await EditingTag.UpdateAsync(_ => null, ct);
        await TagsVersion.UpdateAsync(version => version + 1, ct);
    }

    public async ValueTask DeleteTag(TagModel tag, CancellationToken ct)
    {
        if (tag.Id is null) return;
        await TagService.DeleteAsync(tag.Id.Value, ct);
        await TagsVersion.UpdateAsync(version => version + 1, ct);
    }

    public async ValueTask StartEdit(TagModel tag, CancellationToken ct) =>
        await EditingTag.UpdateAsync(_ => tag, ct);

    public async ValueTask CancelEdit(CancellationToken ct) =>
        await EditingTag.UpdateAsync(_ => null, ct);
}
