using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.Tags;

internal static class TagCqrsRegistrations
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(SearchTagsQuery), typeof(PagedResponse<TagDto>), typeof(SearchTagsHandler)),
        new(typeof(GetTagByIdQuery), typeof(Result<DefaultResponse<TagDto>>), typeof(GetTagByIdHandler)),
        new(typeof(CreateTagCommand), typeof(Result<DefaultResponse<TagDto>>), typeof(CreateTagHandler)),
        new(typeof(UpdateTagCommand), typeof(Result<DefaultResponse<TagDto>>), typeof(UpdateTagHandler)),
        new(typeof(DeleteTagCommand), typeof(Result), typeof(DeleteTagHandler)),
    ];
}
