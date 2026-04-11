using EF.Common.Contracts;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Application.Contracts;

public interface ITenantBoundaryValidator
{
    Result EnsureTenantBoundary(ILogger logger, Guid? requestTenantId, IReadOnlyCollection<string> roles,
        Guid? entityTenantId, string operation, string entityName, Guid? entityId = null);

    Result PreventTenantChange(ILogger logger, Guid? currentTenantId, Guid? newTenantId,
        string entityName, Guid? entityId = null);
}
