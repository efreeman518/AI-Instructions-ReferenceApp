using EF.Common.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Shared;

/// <summary>
/// Small CQRS helpers that mirror service-layer response envelopes, cache keys, and tenant
/// search filtering so both application styles keep the same observable API behavior.
/// </summary>
internal static class HandlerHelpers
{
    public static DefaultResponse<TDto> BuildResponse<TDto>(TDto? dto) =>
        new() { Item = dto, TenantInfo = null };

    public static Result<DefaultResponse<TDto>> Success<TDto>(TDto? dto) =>
        Result<DefaultResponse<TDto>>.Success(BuildResponse(dto));

    public static Result<DefaultResponse<TDto>> NotFoundResponse<TDto>() =>
        Success<TDto>(default);

    public static string CacheKey(string entityName, Guid id) =>
        entityName + ":" + id;

    public static void EnforceTenantFilter<TFilter>(
        SearchRequest<TFilter> request,
        Guid? requestTenantId,
        IReadOnlyCollection<string> roles,
        ILogger logger,
        string operation)
        where TFilter : DefaultSearchFilter, new()
    {
        if (roles.Contains(AppConstants.ROLE_GLOBAL_ADMIN))
        {
            return;
        }

        request.Filter ??= new TFilter();
        if (request.Filter.TenantId is Guid supplied && supplied != requestTenantId)
        {
            logger.LogTenantFilterManipulation(operation, requestTenantId, supplied);
        }

        request.Filter.TenantId = requestTenantId;
    }
}
