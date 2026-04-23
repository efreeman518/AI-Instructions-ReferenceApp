using EF.Common.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Storage;

namespace TaskFlow.Infrastructure.Storage;

public class NoOpAuditLogRepository(ILogger<NoOpAuditLogRepository> logger) : IAuditLogRepository
{
    public Task AppendAsync<TTenantId>(AuditEntry<string, TTenantId> entry, CancellationToken ct = default)
    {
        logger.LogDebug("NoOp: would persist audit entry {AuditEntryId}", entry.Id);
        return Task.CompletedTask;
    }
}