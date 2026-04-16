using EF.Common.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Services.Rules;

namespace TaskFlow.Application.Services;

internal sealed class TenantBoundaryValidator : ITenantBoundaryValidator
{
    public Result EnsureTenantBoundary(ILogger logger, Guid? requestTenantId,
        IReadOnlyCollection<string> roles, Guid? entityTenantId,
        string operation, string entityName, Guid? entityId = null)
    {
        return ValidationHelper.EnsureTenantBoundary(
            logger, requestTenantId, roles, entityTenantId,
            operation, entityName, entityId);
    }

    public Result EnsureGlobalAdmin(IReadOnlyCollection<string> callerRoles, string operation)
    {
        return ValidationHelper.EnsureGlobalAdmin(callerRoles, operation);
    }

    public Result PreventTenantChange(ILogger logger, Guid? currentTenantId, Guid? newTenantId,
        string entityName, Guid entityId)
    {
        return ValidationHelper.PreventTenantChange(logger, currentTenantId, newTenantId,
            entityName, entityId);
    }
}
