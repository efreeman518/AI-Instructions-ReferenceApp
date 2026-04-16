using EF.Common.Contracts;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services.Rules;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Services;

#pragma warning disable CS9113 // Parameter 'cache' is unread — reserved for future caching
internal class TaskItemTagService(
    ILogger<TaskItemTagService> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemTagRepositoryTrxn repoTrxn,
    ITaskItemTagRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache) : ITaskItemTagService
#pragma warning restore CS9113
{
    private Guid? RequestTenantId => requestContext.TenantId;
    private IReadOnlyCollection<string> RequestRoles => requestContext.Roles;
    private bool IsGlobalAdmin => RequestRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN);

    #region Helpers

    private static DefaultResponse<TaskItemTagDto> BuildResponse(TaskItemTagDto dto) =>
        new() { Item = dto, TenantInfo = null };

    #endregion

    public async Task<Result<DefaultResponse<TaskItemTagDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetTaskItemTagAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<TaskItemTagDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "TaskItemTag:Get", nameof(TaskItemTag), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<TaskItemTagDto>>.Success(BuildResponse(entity.ToDto()));
    }

    public async Task<Result<DefaultResponse<TaskItemTagDto>>> CreateAsync(
        DefaultRequest<TaskItemTagDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;
        dto.TenantId = RequestTenantId ?? Guid.Empty;

        var validation = TaskItemTagStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, dto.TenantId,
            "TaskItemTag:Create", nameof(TaskItemTag));
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId);
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
            "TaskItemTag:Delete", nameof(TaskItemTag), entity.Id);
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
