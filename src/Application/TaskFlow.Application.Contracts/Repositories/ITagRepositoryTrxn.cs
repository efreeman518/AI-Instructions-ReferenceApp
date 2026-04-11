using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ITagRepositoryTrxn : IRepositoryBase
{
    Task<Tag?> GetTagAsync(Guid id, CancellationToken ct = default);
}
