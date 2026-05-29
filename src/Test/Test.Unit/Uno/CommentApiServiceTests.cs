using Moq;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

/// <summary>
/// Validates <c>CommentApiService</c> against <c>MockHttpMessageHandler</c>: CRUD methods map Kiota
/// payloads to the Uno <c>CommentModel</c>.
/// Pure-unit tier: in-process <c>HttpClient</c> with mock handler - no real server.
/// </summary>
[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class CommentApiServiceTests
{
    private MockHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private TaskFlowApiClient _apiClient = null!;
    private CommentApiService _service = null!;

    /// <summary>Prepares per-test fixtures so each test starts from a predictable state.</summary>
    [TestInitialize]
    public void Setup()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://localhost:7200") };
        _apiClient = new TaskFlowApiClient(_httpClient);
        _service = new CommentApiService(_apiClient, Mock.Of<INotificationService>());
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
        Assert.AreEqual("Looking good so far!", results[0].Body);
    }

    /// <summary>Verifies get returns mapped model behavior and protects the expected test contract.</summary>
    [TestMethod]
    public async Task GetAsync_ReturnsMappedModel()
    {
        var commentId = Guid.Parse("44444444-4444-4444-4444-111111111111");

        var result = await _service.GetAsync(commentId);

        Assert.IsNotNull(result);
        Assert.AreEqual("Looking good so far!", result.Body);
        Assert.AreEqual(commentId, result.Id);
    }

    /// <summary>Creates returns mapped model used by the surrounding test cases.</summary>
    [TestMethod]
    public async Task CreateAsync_ReturnsMappedModel()
    {
        var taskId = Guid.Parse("33333333-3333-3333-3333-111111111111");
        var newComment = new CommentModel { Body = "New comment", TaskItemId = taskId };

        var result = await _service.CreateAsync(newComment);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
    }

    /// <summary>Verifies update returns mapped model behavior and protects the expected test contract.</summary>
    [TestMethod]
    public async Task UpdateAsync_ReturnsMappedModel()
    {
        var comment = new CommentModel
        {
            Id = Guid.Parse("44444444-4444-4444-4444-111111111111"),
            Body = "Updated comment",
            TaskItemId = Guid.Parse("33333333-3333-3333-3333-111111111111")
        };

        var result = await _service.UpdateAsync(comment);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Id);
    }

    /// <summary>Verifies delete does not throw behavior and protects the expected test contract.</summary>
    [TestMethod]
    public async Task DeleteAsync_DoesNotThrow()
    {
        await _service.DeleteAsync(Guid.NewGuid());
    }
}
