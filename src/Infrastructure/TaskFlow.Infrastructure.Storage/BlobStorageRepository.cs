using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Contracts.Storage;

namespace TaskFlow.Infrastructure.Storage;

public class BlobStorageRepository : IBlobStorageRepository
{
    private readonly BlobServiceClient _client;
    private readonly ILogger<BlobStorageRepository> _logger;

    public BlobStorageRepository(
        IAzureClientFactory<BlobServiceClient> clientFactory,
        IOptions<BlobStorageSettings> settings,
        ILogger<BlobStorageRepository> logger)
    {
        _client = clientFactory.CreateClient("TaskFlowBlobClient");
        _logger = logger;
    }

    public async Task UploadAsync(string containerName, string blobName, Stream content,
        string? contentType = null, IDictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blob = container.GetBlobClient(blobName);

        var options = new BlobUploadOptions();
        if (contentType is not null)
        {
            options.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        }
        if (metadata is not null)
        {
            options.Metadata = metadata;
        }

        await blob.UploadAsync(content, options, ct);
        _logger.LogInformation("Uploaded blob {Container}/{BlobName}", containerName, blobName);
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName,
        CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string containerName, string blobName,
        CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
        _logger.LogInformation("Deleted blob {Container}/{BlobName}", containerName, blobName);
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName,
        CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        var response = await blob.ExistsAsync(ct);
        return response.Value;
    }

    public Task<Uri> GetBlobUriAsync(string containerName, string blobName,
        CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        return Task.FromResult(blob.Uri);
    }
}
