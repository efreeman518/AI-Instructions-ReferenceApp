using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ICategoryRepositoryTrxn : IRepositoryBase
{
    Task<Category?> GetCategoryAsync(Guid id, CancellationToken ct = default);
}
