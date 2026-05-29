using Microsoft.Extensions.Logging;

namespace TaskFlow.Application.Cqrs.Shared;

/// <summary>Provides tenant boundary logging extensions behavior for the Features Shared layer.</summary>
internal static partial class TenantBoundaryLoggingExtensions
{
    /// <summary>Provides the log validation failure operation for tenant boundary logging extensions.</summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation failure in {Context}. Messages={Messages}")]
    public static partial void LogValidationFailure(this ILogger logger, string context, string messages);

    /// <summary>Provides the log tenant filter manipulation operation for tenant boundary logging extensions.</summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Potential tenant filter manipulation in {Context}. RequestTenant={RequestTenantId} SuppliedTenant={SuppliedTenantId}")]
    public static partial void LogTenantFilterManipulation(this ILogger logger, string context, Guid? requestTenantId, Guid? suppliedTenantId);

    /// <summary>Provides the log tenant change attempt operation for tenant boundary logging extensions.</summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Attempted tenant change on {Entity} {EntityId}. ExistingTenant={ExistingTenantId} IncomingTenant={IncomingTenantId}")]
    public static partial void LogTenantChangeAttempt(this ILogger logger, string entity, Guid? entityId, Guid? existingTenantId, Guid? incomingTenantId);
}
