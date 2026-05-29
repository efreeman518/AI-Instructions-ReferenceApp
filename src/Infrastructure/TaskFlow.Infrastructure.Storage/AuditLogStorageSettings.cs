using EF.Table;

namespace TaskFlow.Infrastructure.Storage;

/// <summary>Provides audit log storage behavior for the Infrastructure layer.</summary>
public class AuditLogStorageSettings : TableRepositorySettingsBase
{
    public const string ConfigSectionName = "AuditLogStorageSettings";

    public string TableName { get; set; } = "taskflowaudit";
    public string NullTenantPartitionKey { get; set; } = "_system";

    /// <summary>Initializes audit log storage settings with required dependencies and default state.</summary>
    public AuditLogStorageSettings()
    {
        TableServiceClientName = "TaskFlowTableClient";
    }
}
