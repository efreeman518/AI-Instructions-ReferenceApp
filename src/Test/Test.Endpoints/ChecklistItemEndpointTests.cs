using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Endpoints;

/// <summary>
/// HTTP contract tests for <c>/api/v1/checklist-items</c> CRUD; each test seeds a parent TaskItem via the
/// real API surface to satisfy the foreign-key relation.
/// Endpoint tier (WebApplicationFactory + EF InMemory via <c>CustomApiFactory</c>): contract-level
/// coverage of routing, status codes, and envelope shape - not FK cascade semantics.
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

    /// <summary>Initializes shared test fixtures before the class-level test run begins.</summary>
    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomApiFactory();

    /// <summary>Disposes shared test fixtures after the class-level test run finishes.</summary>
    [ClassCleanup]
    public static void ClassCleanup() => _factory?.Dispose();

    /// <summary>Creates client used by the surrounding test cases.</summary>
    private HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>Creates parent task item used by the surrounding test cases.</summary>
    private async Task<Guid> CreateParentTaskItem(HttpClient client)
    {
        var dto = new TaskItemDto { Title = "ParentForChecklist", Priority = Priority.Medium };
        var response = await client.PostAsJsonAsync("/api/v1/task-items", new DefaultRequest<TaskItemDto> { Item = dto });
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_jsonOptions))!.Item;
        return created!.Id!.Value;
    }

    /// <summary>Verifies that given valid payload, when post checklist item, then returns 201.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_PostChecklistItem_Then_Returns201()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new ChecklistItemDto { Title = "Step 1", TaskItemId = taskId, SortOrder = 0 };

        var response = await client.PostAsJsonAsync("/api/v1/checklist-items", new DefaultRequest<ChecklistItemDto> { Item = dto });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("Step 1", created.Title);
    }

    /// <summary>Verifies that given existing checklist item, when get by ID, then returns 200.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingChecklistItem_When_GetById_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new ChecklistItemDto { Title = "GetStep", TaskItemId = taskId, SortOrder = 1 };
        var createResponse = await client.PostAsJsonAsync("/api/v1/checklist-items", new DefaultRequest<ChecklistItemDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;

        var response = await client.GetAsync($"/api/v1/checklist-items/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;
        Assert.IsNotNull(result);
        Assert.AreEqual("GetStep", result.Title);
    }

    /// <summary>Verifies that given non existent ID, when get checklist item, then returns 404.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetChecklistItem_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/checklist-items/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that given existing checklist item, when put update, then returns 200.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingChecklistItem_When_PutUpdate_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new ChecklistItemDto { Title = "Before step", TaskItemId = taskId, SortOrder = 0 };
        var createResponse = await client.PostAsJsonAsync("/api/v1/checklist-items", new DefaultRequest<ChecklistItemDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;

        var updateDto = new ChecklistItemDto
        {
            Id = created!.Id,
            Title = "After step",
            TaskItemId = taskId,
            SortOrder = 1,
            IsCompleted = true
        };
        var response = await client.PutAsJsonAsync($"/api/v1/checklist-items/{created.Id}", new DefaultRequest<ChecklistItemDto> { Item = updateDto });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;
        Assert.AreEqual("After step", updated!.Title);
    }

    /// <summary>Verifies that given existing checklist item, when delete, then returns 204.</summary>
    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingChecklistItem_When_Delete_Then_Returns204()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new ChecklistItemDto { Title = "ToDelete step", TaskItemId = taskId, SortOrder = 0 };
        var createResponse = await client.PostAsJsonAsync("/api/v1/checklist-items", new DefaultRequest<ChecklistItemDto> { Item = dto });
        var created = (await createResponse.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>())!.Item;

        var response = await client.DeleteAsync($"/api/v1/checklist-items/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/checklist-items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
