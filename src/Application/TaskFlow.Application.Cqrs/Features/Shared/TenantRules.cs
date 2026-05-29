using EF.Common.Contracts;

namespace TaskFlow.Application.Cqrs.Shared;

/// <summary>Provides tenant rules behavior for the Features Shared layer.</summary>
public static class TenantRules
{
    /// <summary>Provides the prevent tenant change operation for tenant rules.</summary>
    public static Result PreventTenantChange(Guid existingTenantId, Guid incomingTenantId, string entityName)
        => existingTenantId == incomingTenantId
            ? Result.Success()
            : Result.Failure($"TenantId cannot be changed for an existing {entityName}.");
}
