using EF.Common.Contracts;

namespace TaskFlow.Application.Cqrs.Shared;

public static class TenantRules
{
    public static Result PreventTenantChange(Guid existingTenantId, Guid incomingTenantId, string entityName)
        => existingTenantId == incomingTenantId
            ? Result.Success()
            : Result.Failure($"TenantId cannot be changed for an existing {entityName}.");
}
