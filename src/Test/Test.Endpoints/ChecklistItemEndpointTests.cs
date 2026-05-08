using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Endpoints;

/// <summary>
/// HTTP contract tests for <c>/api/checklist-items</c> CRUD; each test seeds a parent TaskItem via the
/// real API surface to satisfy the foreign-key relation.
/// Endpoint tier (WebApplicationFactory + EF InMemory via <c>CustomApiFactory</c>): contract-level
/// coverage of routing, status codes, and envelope shape — not FK cascade semantics.
/// </summary>
[TestClass]
public class ChecklistItemEndpointTests
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
        var dto = new TaskItemDto { Title = "ParentForChecklist", Priority = Priority.Medium };
        var response = await client.PostAsJsonAsync("/api/task-items", new DefaultRequest<TaskItemDto> { Item = dto });
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;
        return created!.Id!.Value;
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_PostChecklistItem_Then_Returns201()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new ChecklistItemDto { Title = "Step 1", TaskItemId = taskId, SortOrder = 0 };

        var response = await client.PostAsJsonAsync("/api/checklist-items", new DefaultRequest<ChecklistItemDto> { Item = dto });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("Step 1", created.Title);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingChecklistItem_When_GetById_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new ChecklistItemDto { Title = "GetStep", TaskItemId = taskId, SortOrder = 1 };
        var createResponse = await client.PostAsJsonAsync("/api/checklist-items", new DefaultRequest<ChecklistItemDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;

        var response = await client.GetAsync($"/api/checklist-items/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;
        Assert.IsNotNull(result);
        Assert.AreEqual("GetStep", result.Title);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetChecklistItem_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/checklist-items/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingChecklistItem_When_PutUpdate_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new ChecklistItemDto { Title = "Before step", TaskItemId = taskId, SortOrder = 0 };
        var createResponse = await client.PostAsJsonAsync("/api/checklist-items", new DefaultRequest<ChecklistItemDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;

        var updateDto = new ChecklistItemDto
        {
            Id = created!.Id,
            Title = "After step",
            TaskItemId = taskId,
            SortOrder = 1,
            IsCompleted = true
        };
        var response = await client.PutAsJsonAsync($"/api/checklist-items/{created.Id}", new DefaultRequest<ChecklistItemDto> { Item = updateDto });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;
        Assert.AreEqual("After step", updated!.Title);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingChecklistItem_When_Delete_Then_Returns204()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new ChecklistItemDto { Title = "ToDelete step", TaskItemId = taskId, SortOrder = 0 };
        var createResponse = await client.PostAsJsonAsync("/api/checklist-items", new DefaultRequest<ChecklistItemDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;

        var response = await client.DeleteAsync($"/api/checklist-items/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/checklist-items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
