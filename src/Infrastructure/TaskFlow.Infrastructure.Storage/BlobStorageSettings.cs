using EF.Storage;

namespace TaskFlow.Infrastructure.Storage;

/// <summary>Provides blob storage behavior for the Infrastructure layer.</summary>
public class BlobStorageSettings : BlobRepositorySettingsBase
{
    public string ContainerName { get; set; } = "attachments";

    /// <summary>Initializes blob storage settings with required dependencies and default state.</summary>
    public BlobStorageSettings()
    {
        BlobServiceClientName = "TaskFlowBlobClient";
    }
}
