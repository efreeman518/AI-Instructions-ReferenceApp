using EF.Common.Contracts;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services.Rules;

namespace TaskFlow.Application.Services;

internal class TaskItemTagService(
    ILogger<TaskItemTagService> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemTagRepositoryTrxn repoTrxn,
    ITaskItemTagRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache) : ITaskItemTagService
{
    private Guid? RequestTenantId => requestContext.TenantId;
    private IReadOnlyCollection<string> RequestRoles => requestContext.Roles;
    private bool IsGlobalAdmin => RequestRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN);

    public async Task<Result<DefaultResponse<TaskItemTagDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetTaskItemTagAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<TaskItemTagDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "TaskItemTag:Get", "TaskItemTag", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<TaskItemTagDto>>.Success(new() { Item = entity.ToDto() });
    }

    public async Task<Result<DefaultResponse<TaskItemTagDto>>> CreateAsync(
        DefaultRequest<TaskItemTagDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = TaskItemTagStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, RequestTenantId,
            "TaskItemTag:Create", "TaskItemTag");
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(RequestTenantId ?? Guid.Empty);
        if (entityResult.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating TaskItemTag");
            return Result<DefaultResponse<TaskItemTagDto>>.Failure(ex.GetBaseException().Message);
        }

        return Result<DefaultResponse<TaskItemTagDto>>.Success(new() { Item = entity.ToDto() });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemTagAsync(id, ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "TaskItemTag:Delete", "TaskItemTag", entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting TaskItemTag {Id}", id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        return Result.Success();
    }
}
