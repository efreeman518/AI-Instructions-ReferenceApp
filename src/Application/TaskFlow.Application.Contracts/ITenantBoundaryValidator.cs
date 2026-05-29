using EF.Common.Contracts;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Application.Contracts;

/// <summary>Defines the tenant boundary validator contract used by TaskFlow components.</summary>
public interface ITenantBoundaryValidator
{
    /// <summary>Validates ensure tenant boundary rules and returns failures before work continues.</summary>
    Result EnsureTenantBoundary(ILogger logger, Guid? requestTenantId, IReadOnlyCollection<string> roles,
        Guid? entityTenantId, string operation, string entityName, Guid? entityId = null);

    /// <summary>Validates ensure global admin rules and returns failures before work continues.</summary>
    Result EnsureGlobalAdmin(IReadOnlyCollection<string> callerRoles, string operation);

    /// <summary>Provides the prevent tenant change operation for tenant boundary validator.</summary>
    Result PreventTenantChange(ILogger logger, Guid? currentTenantId, Guid? newTenantId,
        string entityName, Guid entityId);
}
