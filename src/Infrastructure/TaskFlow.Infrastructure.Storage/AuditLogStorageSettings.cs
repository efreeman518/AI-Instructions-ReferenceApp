namespace TaskFlow.Infrastructure.Storage;

public class AuditLogStorageSettings
{
    public const string ConfigSectionName = "AuditLogStorageSettings";

    public string TableName { get; set; } = "taskflowaudit";
    public string NullTenantPartitionKey { get; set; } = "_system";
}