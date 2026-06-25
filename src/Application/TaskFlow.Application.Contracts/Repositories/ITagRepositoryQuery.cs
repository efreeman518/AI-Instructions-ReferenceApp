using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i tag data through infrastructure storage contracts.</summary>
public interface ITagRepositoryQuery : IRepositoryQuery<Tag, TagId>
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Tag?> GetTagAsync(TagId id, CancellationToken ct = default);
    /// <summary>Searches search tags and returns filtered results for callers.</summary>
    Task<PagedResponse<TagDto>> SearchTagsAsync(SearchRequest<TagSearchFilter> request, CancellationToken ct = default);
}
