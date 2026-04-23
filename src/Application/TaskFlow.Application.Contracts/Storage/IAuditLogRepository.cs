using EF.Common.Contracts;

namespace TaskFlow.Application.Contracts.Storage;

public interface IAuditLogRepository
{
    Task AppendAsync<TTenantId>(AuditEntry<string, TTenantId> entry, CancellationToken ct = default);
}