using System.Net;
using System.Net.Http.Json;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Endpoints;

[TestClass]
public class CommentEndpointTests
{
    private static CustomApiFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomApiFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory?.Dispose();

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<Guid> CreateParentTaskItem(HttpClient client)
    {
        var dto = new TaskItemDto { Title = "ParentForComment", Priority = Priority.Medium };
        var response = await client.PostAsJsonAsync("/api/task-items", dto);
        var created = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        return created!.Id!.Value;
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ValidPayload_When_PostComment_Then_Returns201()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new CommentDto { Body = "Test comment", TaskItemId = taskId };

        var response = await client.PostAsJsonAsync("/api/comments", dto);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.IsNotNull(created);
        Assert.AreEqual("Test comment", created.Body);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingComment_When_GetById_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new CommentDto { Body = "GetComment body", TaskItemId = taskId };
        var createResponse = await client.PostAsJsonAsync("/api/comments", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<CommentDto>();

        var response = await client.GetAsync($"/api/comments/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.IsNotNull(result);
        Assert.AreEqual("GetComment body", result.Body);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_NonExistentId_When_GetComment_Then_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/comments/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingComment_When_PutUpdate_Then_Returns200()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new CommentDto { Body = "Before update", TaskItemId = taskId };
        var createResponse = await client.PostAsJsonAsync("/api/comments", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<CommentDto>();

        var updateDto = new CommentDto { Id = created!.Id, Body = "After update", TaskItemId = taskId };
        var response = await client.PutAsJsonAsync($"/api/comments/{created.Id}", updateDto);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.AreEqual("After update", updated!.Body);
    }

    [TestCategory("Endpoint")]
    [TestMethod]
    public async Task Given_ExistingComment_When_Delete_Then_Returns204()
    {
        using var client = CreateClient();
        var taskId = await CreateParentTaskItem(client);
        var dto = new CommentDto { Body = "ToDelete comment", TaskItemId = taskId };
        var createResponse = await client.PostAsJsonAsync("/api/comments", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<CommentDto>();

        var response = await client.DeleteAsync($"/api/comments/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/comments/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
