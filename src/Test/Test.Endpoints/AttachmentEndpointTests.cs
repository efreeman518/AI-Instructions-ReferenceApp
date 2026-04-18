using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Contracts.Storage;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Endpoints;

[TestClass]
public class AttachmentEndpointTests
{
    private static CustomApiFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomApiFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory?.Dispose();

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<Guid> CreateParentTaskItem(HttpClient client)
    {
        var dto = new TaskItemDto { Title = "ParentForAttachment", Priority = Priority.Medium };
        var response = await client.PostAsJsonAsync("/api/task-items", new DefaultRequest<TaskItemDto> { Item = dto });
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;
        return created!.Id!.Value;
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_PostAttachment_Then_Returns201()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new AttachmentDto
        {
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            StorageUri = "https://storage.example.com/test.pdf",
            OwnerType = AttachmentOwnerType.TaskItem,
            OwnerId = taskId
        };

        var response = await client.PostAsJsonAsync("/api/attachments", new DefaultRequest<AttachmentDto> { Item = dto });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<AttachmentDto>>(_jsonOptions))!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("test.pdf", created.FileName);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingAttachment_When_GetById_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new AttachmentDto
        {
            FileName = "get-test.docx",
            ContentType = "application/msword",
            FileSizeBytes = 2048,
            StorageUri = "https://storage.example.com/get-test.docx",
            OwnerType = AttachmentOwnerType.TaskItem,
            OwnerId = taskId
        };
        var createResponse = await client.PostAsJsonAsync("/api/attachments", new DefaultRequest<AttachmentDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<AttachmentDto>>(_jsonOptions))!.Item;

        var response = await client.GetAsync($"/api/attachments/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<DefaultResponse<AttachmentDto>>(_jsonOptions))!.Item;
        Assert.IsNotNull(result);
        Assert.AreEqual("get-test.docx", result.FileName);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetAttachment_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/attachments/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingAttachment_When_PutUpdate_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new AttachmentDto
        {
            FileName = "before.png",
            ContentType = "image/png",
            FileSizeBytes = 512,
            StorageUri = "https://storage.example.com/before.png",
            OwnerType = AttachmentOwnerType.TaskItem,
            OwnerId = taskId
        };
        var createResponse = await client.PostAsJsonAsync("/api/attachments", new DefaultRequest<AttachmentDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<AttachmentDto>>(_jsonOptions))!.Item;

        var updateDto = new AttachmentDto
        {
            Id = created!.Id,
            FileName = "after.png",
            ContentType = "image/png",
            FileSizeBytes = 1024,
            StorageUri = "https://storage.example.com/after.png",
            OwnerType = AttachmentOwnerType.TaskItem,
            OwnerId = taskId
        };
        var response = await client.PutAsJsonAsync($"/api/attachments/{created.Id}", new DefaultRequest<AttachmentDto> { Item = updateDto });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<DefaultResponse<AttachmentDto>>(_jsonOptions))!.Item;
        Assert.AreEqual("after.png", updated!.FileName);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingAttachment_When_Delete_Then_Returns204()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new AttachmentDto
        {
            FileName = "todelete.txt",
            ContentType = "text/plain",
            FileSizeBytes = 256,
            StorageUri = "https://storage.example.com/todelete.txt",
            OwnerType = AttachmentOwnerType.TaskItem,
            OwnerId = taskId
        };
        var createResponse = await client.PostAsJsonAsync("/api/attachments", new DefaultRequest<AttachmentDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<AttachmentDto>>(_jsonOptions))!.Item;

        var response = await client.DeleteAsync($"/api/attachments/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/attachments/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_FileUpload_When_PostUpload_Then_Returns201WithBlobUri()
    {
        // Create factory with in-memory blob storage
        using var uploadFactory = new CustomApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IBlobStorageRepository>(new InMemoryBlobStorageRepository());
            });
        });
        using var client = uploadFactory.CreateClient();
        var taskId = await CreateParentTaskItem(client);

        using var content = new MultipartFormDataContent();
        var fileBytes = "Hello, blob!"u8.ToArray();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "upload-test.txt");
        content.Add(new StringContent(((int)AttachmentOwnerType.TaskItem).ToString()), "ownerType");
        content.Add(new StringContent(taskId.ToString()), "ownerId");

        var response = await client.PostAsync("/api/attachments/upload", content);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<AttachmentDto>>(_jsonOptions))!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("upload-test.txt", created.FileName);
        Assert.Contains("upload-test.txt", created.StorageUri);
        Assert.AreEqual(fileBytes.Length, created.FileSizeBytes);
    }
}

internal class InMemoryBlobStorageRepository : IBlobStorageRepository
{
    private readonly Dictionary<string, byte[]> _blobs = new();

    public async Task UploadAsync(string containerName, string blobName, Stream content,
        string? contentType = null, IDictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        _blobs[$"{containerName}/{blobName}"] = ms.ToArray();
    }

    public Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var key = $"{containerName}/{blobName}";
        if (!_blobs.TryGetValue(key, out var data))
            throw new InvalidOperationException($"Blob {key} not found");
        return Task.FromResult<Stream>(new MemoryStream(data));
    }

    public Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        _blobs.Remove($"{containerName}/{blobName}");
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken ct = default)
        => Task.FromResult(_blobs.ContainsKey($"{containerName}/{blobName}"));

    public Task<Uri> GetBlobUriAsync(string containerName, string blobName, CancellationToken ct = default)
        => Task.FromResult(new Uri($"https://inmemory.blob.local/{containerName}/{blobName}"));
}
