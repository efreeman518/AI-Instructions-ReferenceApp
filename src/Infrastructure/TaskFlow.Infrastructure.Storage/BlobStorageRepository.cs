using Azure.Storage.Blobs;
using EF.Storage;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Contracts.Storage;

namespace TaskFlow.Infrastructure.Storage;

public class BlobStorageRepository(
    ILogger<BlobStorageRepository> logger,
    IOptions<BlobStorageSettings> settings,
    IAzureClientFactory<BlobServiceClient> clientFactory)
    : BlobRepositoryBase(logger, Options.Create((BlobRepositorySettingsBase)settings.Value), clientFactory),
      IBlobStorageRepository
{
    private readonly ContainerInfo _container = new()
    {
        ContainerName = settings.Value.ContainerName,
        CreateContainerIfNotExist = true
    };

    public Task UploadAsync(string containerName, string blobName, Stream content,
        string? contentType = null, IDictionary<string, string>? metadata = null,
        CancellationToken ct = default) =>
        UploadBlobStreamAsync(new ContainerInfo { ContainerName = containerName, CreateContainerIfNotExist = true },
            blobName, content, contentType, false, metadata, ct);

    public async Task<Stream> DownloadAsync(string containerName, string blobName,
        CancellationToken ct = default) =>
        await StartDownloadBlobStreamAsync(new ContainerInfo { ContainerName = containerName }, blobName, false, ct);

    public Task DeleteAsync(string containerName, string blobName,
        CancellationToken ct = default) =>
        DeleteBlobAsync(new ContainerInfo { ContainerName = containerName }, blobName, ct);

    public async Task<bool> ExistsAsync(string containerName, string blobName,
        CancellationToken ct = default)
    {
        var info = new ContainerInfo { ContainerName = containerName, CreateContainerIfNotExist = true };
        var (blobs, _) = await QueryPageBlobsAsync(info, prefix: blobName, cancellationToken: ct);
        return blobs.Any(b => b.Name == blobName);
    }

    public async Task<Uri> GetBlobUriAsync(string containerName, string blobName,
        CancellationToken ct = default)
    {
        var sasUri = await GenerateBlobSasUriAsync(
            new ContainerInfo { ContainerName = containerName },
            blobName,
            Azure.Storage.Sas.BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.AddHours(1),
            cancellationToken: ct);
        return sasUri ?? throw new InvalidOperationException($"Could not generate URI for blob {containerName}/{blobName}");
    }
}
