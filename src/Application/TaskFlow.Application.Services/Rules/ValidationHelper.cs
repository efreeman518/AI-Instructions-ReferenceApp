using Microsoft.Extensions.Logging;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts;

namespace TaskFlow.Application.Services.Rules;

internal static class ValidationHelper
{
    public static Result EnsureGlobalAdmin(IReadOnlyCollection<string> callerRoles, string operation)
    {
        return callerRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN)
            ? Result.Success()
            : Result.Failure($"Forbidden: Only a GlobalAdmin may perform this operation: {operation}.");
    }

    public static Result EnsureTenantBoundary(
        ILogger logger,
        Guid? callerTenantId,
        IReadOnlyCollection<string> callerRoles,
        Guid? entityTenantId,
        string operation,
        string entityName,
        Guid? entityId = null)
    {
        if (callerRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN))
            return Result.Success();

        if (callerRoles is null || callerRoles.Count == 0)
        {
            logger.LogWarning(
                "Tenant boundary violation attempt: Caller without roles attempted access. Operation={Operation}, Entity={EntityName}, EntityId={EntityId}",
                operation, entityName, entityId);
            return Result.Failure($"Forbidden: Tenant boundary violation for operation: {operation}.");
        }

        if (entityTenantId is null)
        {
            logger.LogWarning(
                "Tenant boundary violation attempt: Non-GlobalAdmin tried to access a global entity. Operation={Operation}, Entity={EntityName}, EntityId={EntityId}",
                operation, entityName, entityId);
            return Result.Failure($"Forbidden: Tenant boundary violation for operation: {operation}.");
        }

        if (callerTenantId.HasValue && callerTenantId.Value == entityTenantId)
            return Result.Success();

        logger.LogWarning(
            "Tenant boundary violation attempt: CallerTenantId={CallerTenantId}, EntityTenantId={EntityTenantId}, Operation={Operation}, Entity={EntityName}, EntityId={EntityId}",
            callerTenantId, entityTenantId, operation, entityName, entityId);

        return Result.Failure($"Forbidden: Tenant boundary violation for operation: {operation}.");
    }

    public static Result PreventTenantChange(ILogger logger, Guid? existingTenantId, Guid? incomingTenantId, string entityName, Guid entityId)
    {
        if (existingTenantId != incomingTenantId)
        {
            logger.LogTenantChangeAttempt(entityName, entityId, existingTenantId, incomingTenantId);
            return Result.Failure($"TenantId cannot be changed for an existing {entityName}.");
        }
        return Result.Success();
    }
}
