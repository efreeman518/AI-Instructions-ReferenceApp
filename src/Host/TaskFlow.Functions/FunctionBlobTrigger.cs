using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Functions;

/// <summary>Configures function blob trigger host behavior for TaskFlow runtime services.</summary>
public class FunctionBlobTrigger(ILogger<FunctionBlobTrigger> logger)
{
    /// <summary>Processes attachment through function blob trigger.</summary>
    [Function(nameof(ProcessAttachment))]
    public Task ProcessAttachment(
        [BlobTrigger("%AttachmentBlobContainer%/{name}", Connection = "BlobStorage1")] Stream blobStream,
        string name,
        CancellationToken ct)
    {
        logger.LogInformation("Blob trigger: processing attachment '{Name}', size {Size} bytes",
            name, blobStream.Length);

        // Future: validate file, extract metadata, update Attachment entity
        return Task.CompletedTask;
    }
}
