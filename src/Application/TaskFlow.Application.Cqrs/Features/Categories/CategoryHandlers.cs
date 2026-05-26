using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Cqrs.Features.Categories;

internal sealed class SearchCategoriesHandler(
    ILogger<SearchCategoriesHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICategoryRepositoryQuery repoQuery)
    : IRequestHandler<SearchCategoriesQuery, PagedResponse<CategoryDto>>
{
    public async Task<PagedResponse<CategoryDto>> HandleAsync(SearchCategoriesQuery query, CancellationToken ct = default)
    {
        var request = query.Request;
        HandlerHelpers.EnforceTenantFilter(request, requestContext.TenantId, requestContext.Roles, logger, "CategorySearch");
        return await CqrsHandlerSupport.SearchAsync(token => repoQuery.SearchCategoriesAsync(request, token), logger, "Category", ct);
    }
}

internal sealed class GetCategoryByIdHandler(
    ILogger<GetCategoryByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICategoryRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetCategoryByIdQuery, Result<DefaultResponse<CategoryDto>>>
{
    public async Task<Result<DefaultResponse<CategoryDto>>> HandleAsync(GetCategoryByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetCategoryAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<CategoryDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Category:Get", nameof(Category), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(boundary.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class CreateCategoryHandler(
    ILogger<CreateCategoryHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICategoryRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<CreateCategoryCommand, Result<DefaultResponse<CategoryDto>>>
{
    public async Task<Result<DefaultResponse<CategoryDto>>> HandleAsync(CreateCategoryCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = CategoryStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, dto.TenantId,
            "Category:Create", nameof(Category));
        if (boundary.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId);
        if (entityResult.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error creating Category", ct);
        if (save.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class UpdateCategoryHandler(
    ILogger<UpdateCategoryHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICategoryRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<UpdateCategoryCommand, Result<DefaultResponse<CategoryDto>>>
{
    public async Task<Result<DefaultResponse<CategoryDto>>> HandleAsync(UpdateCategoryCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = CategoryStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetCategoryAsync(dto.Id!.Value, ct);
        if (entity is null)
        {
            return HandlerHelpers.NotFoundResponse<CategoryDto>();
        }

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Category:Update", nameof(Category), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(boundary.ErrorMessage!);

        var tenantChangeCheck = tenantBoundaryValidator.PreventTenantChange(
            logger, entity.TenantId, dto.TenantId, nameof(Category), entity.Id);
        if (tenantChangeCheck.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(tenantChangeCheck.ErrorMessage!);

        var updateResult = entity.Update(dto.Name, dto.Description, dto.SortOrder, dto.IsActive, dto.ParentCategoryId);
        if (updateResult.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(updateResult.ErrorMessage!);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error updating Category {Id}", ct, dto.Id);
        if (save.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class DeleteCategoryHandler(
    ILogger<DeleteCategoryHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ICategoryRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache)
    : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteCategoryCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetCategoryAsync(command.Id, ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "Category:Delete", nameof(Category), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error deleting Category {Id}", ct, command.Id);
        if (save.IsFailure) return save;

        await cache.RemoveAsync(HandlerHelpers.CacheKey(nameof(Category), command.Id), ct);
        return Result.Success();
    }
}
