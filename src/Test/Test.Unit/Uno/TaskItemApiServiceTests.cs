using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

/// <summary>
/// Validates <c>TaskItemApiService</c> against <c>MockHttpMessageHandler</c> plus a capturing handler
/// that asserts child collections emit non-null <c>taskItemId</c> values when posting a new TaskItem.
/// Pure-unit tier: in-process <c>HttpClient</c> with mock/capture handlers - no real server.
/// </summary>
[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class TaskItemApiServiceTests
{
    private MockHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private TaskFlowApiClient _apiClient = null!;
    private TaskItemApiService _service = null!;

    /// <summary>Prepares per-test fixtures so each test starts from a predictable state.</summary>
    [TestInitialize]
    public void Setup()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:7200") };
        _apiClient = new TaskFlowApiClient(_httpClient);
        _service = new TaskItemApiService(_apiClient, Mock.Of<INotificationService>());
    }

    /// <summary>Verifies teardown behavior and protects the expected test contract.</summary>
    [TestCleanup]
    public void Teardown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    /// <summary>Verifies search returns mapped models behavior and protects the expected test contract.</summary>
    [TestMethod]
    public async Task SearchAsync_ReturnsMappedModels()
    {
        var results = await _service.SearchAsync();

        Assert.IsNotEmpty(results);
        Assert.AreEqual("Build dashboard UI", results[0].Title);
        Assert.AreEqual("InProgress", results[0].Status);
        Assert.AreEqual("High", results[0].Priority);
    }

    /// <summary>Verifies search includes overdue task behavior and protects the expected test contract.</summary>
    [TestMethod]
    public async Task SearchAsync_IncludesOverdueTask()
    {
        var results = await _service.SearchAsync();

        var overdueTask = results.FirstOrDefault(t => t.Title == "Fix login validation");
        Assert.IsNotNull(overdueTask);
        Assert.IsTrue(overdueTask.IsOverdue);
    }

    /// <summary>Creates returns mapped model used by the surrounding test cases.</summary>
    [TestMethod]
    public async Task CreateAsync_ReturnsMappedModel()
    {
        var newTask = new TaskItemModel { Title = "Test task", Priority = "Medium" };

        var result = await _service.CreateAsync(newTask);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
    }

    /// <summary>Creates with child collections does not send null task item ids used by the surrounding test cases.</summary>
    [TestMethod]
    public async Task CreateAsync_WithChildCollections_DoesNotSendNullTaskItemIds()
    {
        var captureHandler = new CaptureRequestHandler();
        using var httpClient = new HttpClient(captureHandler) { BaseAddress = new Uri("https://localhost:7200") };
        var apiClient = new TaskFlowApiClient(httpClient);
        var service = new TaskItemApiService(apiClient, Mock.Of<INotificationService>());

        var model = new TaskItemModel
        {
            Title = "Task with children",
            Priority = "Medium",
            Comments = [new CommentModel { Body = "note", TaskItemId = Guid.Empty }],
            ChecklistItems = [new ChecklistItemModel { Title = "todo", SortOrder = 1, IsCompleted = false, TaskItemId = Guid.Empty }]
        };

        var result = await service.CreateAsync(model);

        Assert.IsNotNull(result);
        Assert.IsNotNull(captureHandler.LastRequestBody);

        using var doc = JsonDocument.Parse(captureHandler.LastRequestBody!);
        var item = doc.RootElement.GetProperty("item");
        var commentTaskItemId = item.GetProperty("comments")[0].GetProperty("taskItemId");
        var checklistTaskItemId = item.GetProperty("checklistItems")[0].GetProperty("taskItemId");

        Assert.AreNotEqual(JsonValueKind.Null, commentTaskItemId.ValueKind);
        Assert.AreNotEqual(JsonValueKind.Null, checklistTaskItemId.ValueKind);
        Assert.AreEqual(Guid.Empty.ToString(), commentTaskItemId.GetString());
        Assert.AreEqual(Guid.Empty.ToString(), checklistTaskItemId.GetString());
    }

    /// <summary>Verifies delete does not throw behavior and protects the expected test contract.</summary>
    [TestMethod]
    public async Task DeleteAsync_DoesNotThrow()
    {
        await _service.DeleteAsync(Guid.NewGuid());
        // No exception = success
    }

    /// <summary>Supports test execution for Test.unit Uno scenarios.</summary>
    private sealed class CaptureRequestHandler : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        /// <summary>Verifies send behavior and protects the expected test contract.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var responseJson = "{\"item\":{\"id\":\"" + Guid.NewGuid() + "\",\"title\":\"Created\",\"priority\":\"Medium\",\"status\":\"Open\"}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
