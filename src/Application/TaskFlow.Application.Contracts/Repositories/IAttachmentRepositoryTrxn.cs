using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i attachment data through infrastructure storage contracts.</summary>
public interface IAttachmentRepositoryTrxn : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Attachment?> GetAttachmentAsync(Guid id, CancellationToken ct = default);
}
