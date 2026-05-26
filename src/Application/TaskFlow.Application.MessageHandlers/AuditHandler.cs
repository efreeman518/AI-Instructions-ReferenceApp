using EF.BackgroundServices.Attributes;
using EF.BackgroundServices.InternalMessageBus;
using EF.Common.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Storage;

namespace TaskFlow.Application.MessageHandlers;

/// <summary>
/// Internal message-bus handler that persists EF audit entries to the configured audit store.
/// It supports both required and nullable tenant-id audit shapes emitted by the shared interceptor.
/// </summary>
[ScopedMessageHandler]
public class AuditHandler(
    ILogger<AuditHandler> logger,
    IAuditLogRepository auditLogRepository) :
    IMessageHandler<AuditEntry<string, Guid>>,
    IMessageHandler<AuditEntry<string, Guid?>>
{
    public Task HandleAsync(AuditEntry<string, Guid> message, CancellationToken cancellationToken = default)
    {
        return HandleCoreAsync(message, cancellationToken);
    }

    public Task HandleAsync(AuditEntry<string, Guid?> message, CancellationToken cancellationToken = default)
    {
        return HandleCoreAsync(message, cancellationToken);
    }

    private async Task HandleCoreAsync<TTenantId>(AuditEntry<string, TTenantId> message, CancellationToken cancellationToken)
    {
        await auditLogRepository.AppendAsync(message, cancellationToken).ConfigureAwait(false);

        logger.LogDebug(
            "Persisted audit message {AuditEntryId} for {EntityType} {Action}",
            message.Id,
            message.EntityType,
            message.Action);
    }
}
