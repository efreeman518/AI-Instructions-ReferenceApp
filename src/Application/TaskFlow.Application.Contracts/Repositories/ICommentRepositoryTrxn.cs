using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ICommentRepositoryTrxn : IRepositoryBase
{
    Task<Comment?> GetCommentAsync(Guid id, CancellationToken ct = default);
}
