using EF.Table;

namespace TaskFlow.Infrastructure.Storage;

public class AuditLogStorageSettings : TableRepositorySettingsBase
{
    public const string ConfigSectionName = "AuditLogStorageSettings";

    public string TableName { get; set; } = "taskflowaudit";
    public string NullTenantPartitionKey { get; set; } = "_system";

    public AuditLogStorageSettings()
    {
        TableServiceClientName = "TaskFlowTableClient";
    }
}
