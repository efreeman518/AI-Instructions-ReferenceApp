using Microsoft.Extensions.Logging;

namespace TaskFlow.Application.Services.Rules;

internal static partial class TenantBoundaryLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation failure in {Context}. Messages={Messages}")]
    public static partial void LogValidationFailure(this ILogger logger, string context, string messages);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Potential tenant filter manipulation in {Context}. RequestTenant={RequestTenantId} SuppliedTenant={SuppliedTenantId}")]
    public static partial void LogTenantFilterManipulation(this ILogger logger, string context, Guid? requestTenantId, Guid? suppliedTenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Attempted tenant change on {Entity} {EntityId}. ExistingTenant={ExistingTenantId} IncomingTenant={IncomingTenantId}")]
    public static partial void LogTenantChangeAttempt(this ILogger logger, string entity, Guid? entityId, Guid? existingTenantId, Guid? incomingTenantId);
}
