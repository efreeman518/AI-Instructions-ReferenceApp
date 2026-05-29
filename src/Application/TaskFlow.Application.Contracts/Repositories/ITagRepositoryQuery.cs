using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i tag data through infrastructure storage contracts.</summary>
public interface ITagRepositoryQuery : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Tag?> GetTagAsync(Guid id, CancellationToken ct = default);
    /// <summary>Searches search tags and returns filtered results for callers.</summary>
    Task<PagedResponse<TagDto>> SearchTagsAsync(SearchRequest<TagSearchFilter> request, CancellationToken ct = default);
}
