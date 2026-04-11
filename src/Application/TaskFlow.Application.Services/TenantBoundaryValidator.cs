using EF.Common.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;

namespace TaskFlow.Application.Services;

internal class TenantBoundaryValidator : ITenantBoundaryValidator
{
    public Result EnsureTenantBoundary(ILogger logger, Guid? requestTenantId,
        IReadOnlyCollection<string> roles, Guid? entityTenantId,
        string operation, string entityName, Guid? entityId = null)
    {
        // GlobalAdmin bypass
        if (roles.Contains(AppConstants.ROLE_GLOBAL_ADMIN))
            return Result.Success();

        if (requestTenantId == null)
            return Result.Failure($"Tenant context required for {operation} on {entityName}.");

        if (entityTenantId != null && entityTenantId != requestTenantId)
            return Result.Failure($"Tenant boundary violation: {operation} on {entityName} {entityId}.");

        return Result.Success();
    }

    public Result PreventTenantChange(ILogger logger, Guid? currentTenantId, Guid? newTenantId,
        string entityName, Guid? entityId = null)
    {
        if (currentTenantId != newTenantId)
            return Result.Failure($"Cannot change tenant for {entityName} {entityId}.");

        return Result.Success();
    }
}
