using Azure.Data.Tables;
using EF.Common.Contracts;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Contracts.Storage;

namespace TaskFlow.Infrastructure.Storage;

// Uses IAzureClientFactory directly because TableRepositoryBase uses typeof(T).Name as the table name,
// but audit log requires a configurable table name from settings.
public class AuditLogRepository(
    IAzureClientFactory<TableServiceClient> clientFactory,
    IOptions<AuditLogStorageSettings> settings,
    ILogger<AuditLogRepository> logger) : IAuditLogRepository
{
    private readonly TableServiceClient _client = clientFactory.CreateClient(settings.Value.TableServiceClientName);
    private readonly AuditLogStorageSettings _settings = settings.Value;
    private readonly ILogger<AuditLogRepository> _logger = logger;

    public async Task AppendAsync<TTenantId>(AuditEntry<string, TTenantId> entry, CancellationToken ct = default)
    {
        var table = _client.GetTableClient(_settings.TableName);
        await table.CreateIfNotExistsAsync(ct).ConfigureAwait(false);

        var recordedUtc = DateTimeOffset.UtcNow;
        var tenantId = GetTenantId(entry.TenantId);
        var partitionKey = string.IsNullOrWhiteSpace(tenantId)
            ? _settings.NullTenantPartitionKey
            : tenantId;
        var rowKey = $"{DateTime.MaxValue.Ticks - recordedUtc.UtcDateTime.Ticks:D19}_{entry.Id:N}";

        var entity = new AuditLogTableEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            Id = entry.Id,
            AuditId = entry.AuditId,
            TenantId = tenantId,
            EntityType = entry.EntityType,
            EntityKey = entry.EntityKey,
            Action = entry.Action,
            Status = entry.Status.ToString(),
            StartTimeTicks = entry.StartTime.Ticks,
            ElapsedTimeTicks = entry.ElapsedTime.Ticks,
            RecordedUtc = recordedUtc,
            Metadata = entry.Metadata,
            Error = entry.Error
        };

        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Persisted audit entry {AuditEntryId} for tenant {TenantId} entity {EntityType} action {Action}",
            entry.Id,
            partitionKey,
            entry.EntityType,
            entry.Action);
    }

    private static string? GetTenantId<TTenantId>(TTenantId tenantId)
    {
        object? tenantValue = tenantId;
        return tenantValue switch
        {
            null => null,
            Guid value when value == Guid.Empty => null,
            _ => tenantValue.ToString()
        };
    }
}
