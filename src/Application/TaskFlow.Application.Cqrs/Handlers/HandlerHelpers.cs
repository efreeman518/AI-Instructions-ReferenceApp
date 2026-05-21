using EF.Common.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Cqrs.Validation;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Handlers;

internal static class HandlerHelpers
{
    public static DefaultResponse<TDto> BuildResponse<TDto>(TDto? dto) =>
        new() { Item = dto, TenantInfo = null };

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
