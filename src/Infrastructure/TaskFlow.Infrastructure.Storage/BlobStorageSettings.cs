using EF.Storage;

namespace TaskFlow.Infrastructure.Storage;

public class BlobStorageSettings : BlobRepositorySettingsBase
{
    public string ContainerName { get; set; } = "attachments";

    public BlobStorageSettings()
    {
        BlobServiceClientName = "TaskFlowBlobClient";
    }
}
