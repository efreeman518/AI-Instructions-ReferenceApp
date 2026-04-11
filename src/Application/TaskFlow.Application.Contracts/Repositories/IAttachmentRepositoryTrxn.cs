using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface IAttachmentRepositoryTrxn : IRepositoryBase
{
    Task<Attachment?> GetAttachmentAsync(Guid id, CancellationToken ct = default);
}
