using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Functions;

public class FunctionBlobTrigger(ILogger<FunctionBlobTrigger> logger)
{
    [Function(nameof(ProcessAttachment))]
    public Task ProcessAttachment(
        [BlobTrigger("%AttachmentBlobContainer%/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
        string name,
        CancellationToken ct)
    {
        logger.LogInformation("Blob trigger: processing attachment '{Name}', size {Size} bytes",
            name, blobStream.Length);

        // Future: validate file, extract metadata, update Attachment entity
        return Task.CompletedTask;
    }
}
