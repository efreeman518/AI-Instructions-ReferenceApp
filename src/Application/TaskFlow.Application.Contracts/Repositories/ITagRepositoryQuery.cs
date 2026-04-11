using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ITagRepositoryQuery : IRepositoryBase
{
    Task<Tag?> GetTagAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<Tag>> SearchTagsAsync(SearchRequest<TagSearchFilter> request, CancellationToken ct = default);
}
