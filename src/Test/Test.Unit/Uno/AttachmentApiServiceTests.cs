using Moq;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

/// <summary>
/// Validates <c>AttachmentApiService</c> against the in-memory <c>MockHttpMessageHandler</c>: search,
/// get, create, and delete map the Kiota response shape to the Uno <c>AttachmentModel</c> correctly.
/// Pure-unit tier: a real <c>HttpClient</c> is used but no real server — the mock handler short-circuits.
/// </summary>
[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class AttachmentApiServiceTests
{
    private MockHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private TaskFlowApiClient _apiClient = null!;
    private AttachmentApiService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:7200") };
        _apiClient = new TaskFlowApiClient(_httpClient);
        _service = new AttachmentApiService(_apiClient, Mock.Of<INotificationService>());
    }

    [TestCleanup]
    public void Teardown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [TestMethod]
    public async Task SearchAsync_ReturnsMappedModels()
    {
        var results = await _service.SearchAsync();

        Assert.IsNotEmpty(results);
        Assert.AreEqual("design.pdf", results[0].FileName);
        Assert.AreEqual("application/pdf", results[0].ContentType);
        Assert.AreEqual(4096, results[0].FileSizeBytes);
    }

    [TestMethod]
    public async Task GetAsync_ReturnsMappedModel()
    {
        var attachmentId = Guid.Parse("66666666-6666-6666-6666-111111111111");

        var result = await _service.GetAsync(attachmentId);

        Assert.IsNotNull(result);
        Assert.AreEqual("design.pdf", result.FileName);
        Assert.AreEqual("TaskItem", result.OwnerType);
    }

    [TestMethod]
    public async Task CreateAsync_ReturnsMappedModel()
    {
        var taskId = Guid.Parse("33333333-3333-3333-3333-111111111111");
        var newAttachment = new AttachmentModel
        {
            FileName = "report.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileSizeBytes = 8192,
            StorageUri = "https://storage.example.com/report.xlsx",
            OwnerType = "TaskItem",
            OwnerId = taskId
        };

        var result = await _service.CreateAsync(newAttachment);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
    }

    [TestMethod]
    public async Task DeleteAsync_DoesNotThrow()
    {
        await _service.DeleteAsync(Guid.NewGuid());
    }
}
