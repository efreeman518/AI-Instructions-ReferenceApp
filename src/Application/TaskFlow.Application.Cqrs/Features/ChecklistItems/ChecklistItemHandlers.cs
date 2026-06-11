using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Cqrs.Features.ChecklistItems;

/// <summary>Handles search checklist items work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class SearchChecklistItemsHandler(
    ILogger<SearchChecklistItemsHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryQuery repoQuery)
    : IRequestHandler<SearchChecklistItemsQuery, PagedResponse<ChecklistItemDto>>
{
    /// <summary>Handles search checklist items requests and returns the application result.</summary>
    public async Task<PagedResponse<ChecklistItemDto>> HandleAsync(SearchChecklistItemsQuery query, CancellationToken ct = default)
    {
        var request = query.Request;
        HandlerHelpers.EnforceTenantFilter(request, requestContext.TenantId, requestContext.Roles, logger, "ChecklistItemSearch");
        return await CqrsHandlerSupport.SearchAsync(token => repoQuery.SearchChecklistItemsAsync(request, token), logger, "ChecklistItem", ct);
    }
}

/// <summary>Handles get checklist item by ID work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class GetChecklistItemByIdHandler(
    ILogger<GetChecklistItemByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetChecklistItemByIdQuery, Result<DefaultResponse<ChecklistItemDto>>>
{
    /// <summary>Handles get checklist item by ID requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<ChecklistItemDto>>> HandleAsync(GetChecklistItemByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetChecklistItemAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<ChecklistItemDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "ChecklistItem:Get", nameof(ChecklistItem), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}
