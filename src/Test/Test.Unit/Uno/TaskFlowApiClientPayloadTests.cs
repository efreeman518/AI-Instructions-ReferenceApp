using System.Net;
using System.Text;
using System.Text.Json;
using TaskFlow.Uno.Core.Client;

namespace Test.Unit.Uno;

/// <summary>
/// Validates the Kiota-generated <c>TaskFlowApiClient</c> outgoing payload shape: child collections
/// (Comments, ChecklistItems) always emit a non-null <c>taskItemId</c> on POST and reuse the route id on
/// PUT, even when the caller leaves them empty.
/// Pure-unit tier: a capturing <see cref="System.Net.Http.HttpMessageHandler"/> records the JSON body
/// without ever opening a socket — payload-shape regression coverage for client serialization rules.
/// </summary>
[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class TaskFlowApiClientPayloadTests
{
    [TestMethod]
    public async Task TaskItemsPostAsync_SetsChildTaskItemId_WhenMissing()
    {
        var handler = new CaptureRequestHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7200") };
        var apiClient = new TaskFlowApiClient(httpClient);

        var dto = new TaskItemDto
        {
            Title = "New Task",
            Priority = "Medium",
            Comments = [new CommentDto { Body = "hello" }],
            ChecklistItems = [new ChecklistItemDto { Title = "step 1", IsCompleted = false, SortOrder = 1 }]
        };

        var result = await apiClient.Api.TaskItems.PostAsync(dto);

        Assert.IsNotNull(result);
        Assert.IsNotNull(handler.LastRequestBody);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var commentTaskItemId = doc.RootElement.GetProperty("item").GetProperty("comments")[0].GetProperty("taskItemId");
        var checklistTaskItemId = doc.RootElement.GetProperty("item").GetProperty("checklistItems")[0].GetProperty("taskItemId");

        Assert.AreNotEqual(JsonValueKind.Null, commentTaskItemId.ValueKind);
        Assert.AreNotEqual(JsonValueKind.Null, checklistTaskItemId.ValueKind);
        Assert.AreEqual(Guid.Empty.ToString(), commentTaskItemId.GetString());
        Assert.AreEqual(Guid.Empty.ToString(), checklistTaskItemId.GetString());
    }

    [TestMethod]
    public async Task TaskItemsPutAsync_UsesRouteId_ForMissingChildTaskItemId()
    {
        var handler = new CaptureRequestHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7200") };
        var apiClient = new TaskFlowApiClient(httpClient);

        var taskId = Guid.NewGuid();
        var dto = new TaskItemDto
        {
            Id = taskId,
            Title = "Existing Task",
            Priority = "High",
            Comments = [new CommentDto { Body = "updated" }],
            ChecklistItems = [new ChecklistItemDto { Title = "step 1", IsCompleted = false, SortOrder = 1 }]
        };

        var result = await apiClient.Api.TaskItems[taskId].PutAsync(dto);

        Assert.IsNotNull(result);
        Assert.IsNotNull(handler.LastRequestBody);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var commentTaskItemId = doc.RootElement.GetProperty("item").GetProperty("comments")[0].GetProperty("taskItemId").GetString();
        var checklistTaskItemId = doc.RootElement.GetProperty("item").GetProperty("checklistItems")[0].GetProperty("taskItemId").GetString();

        Assert.AreEqual(taskId.ToString(), commentTaskItemId);
        Assert.AreEqual(taskId.ToString(), checklistTaskItemId);
    }

    private sealed class CaptureRequestHandler : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var responseJson = "{\"item\":{\"id\":\"" + Guid.NewGuid() + "\"}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
