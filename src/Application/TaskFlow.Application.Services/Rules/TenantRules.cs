using EF.Common.Contracts;

namespace TaskFlow.Application.Services.Rules;

/// <summary>Provides tenant rules behavior for the Application Rules layer.</summary>
public static class TenantRules
{
    /// <summary>Provides the prevent tenant change operation for tenant rules.</summary>
    public static Result PreventTenantChange(Guid existingTenantId, Guid incomingTenantId, string entityName)
        => existingTenantId == incomingTenantId
            ? Result.Success()
            : Result.Failure($"TenantId cannot be changed for an existing {entityName}.");
}
