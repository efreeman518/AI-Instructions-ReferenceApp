namespace TaskFlow.Application.Contracts.Storage;

public interface IBlobStorageRepository
{
    Task UploadAsync(string containerName, string blobName, Stream content,
        string? contentType = null, IDictionary<string, string>? metadata = null,
        CancellationToken ct = default);

    Task<Stream> DownloadAsync(string containerName, string blobName,
        CancellationToken ct = default);

    Task DeleteAsync(string containerName, string blobName,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(string containerName, string blobName,
        CancellationToken ct = default);

    Task<Uri> GetBlobUriAsync(string containerName, string blobName,
        CancellationToken ct = default);
}
