using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EF.Common.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.E2E;

[TestClass]
[TestCategory("E2E")]
public class TaskItemCrudE2ETests
{
    private static SqlApiFactory _factory = null!;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        await SqlApiFactory.StartContainerAsync();
        _factory = new SqlApiFactory();

        // Apply EF migrations against the real SQL container
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlow.Infrastructure.Data.TaskFlowDbContextTrxn>();
        await db.Database.MigrateAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        _factory?.Dispose();
        await SqlApiFactory.StopContainerAsync();
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    // ── TaskItem full CRUD ────────────────────────────────────

    [TestMethod]
    public async Task TaskItem_FullCrudCycle_AgainstRealSql()
    {
        using var client = CreateClient();

        // CREATE
        var dto = new TaskItemDto { Title = "E2E Task", Priority = Priority.High };
        var createResp = await client.PostAsJsonAsync("/api/task-items",
            new DefaultRequest<TaskItemDto> { Item = dto });
        Assert.AreEqual(HttpStatusCode.Created, createResp.StatusCode,
            $"Create failed: {await createResp.Content.ReadAsStringAsync()}");
        var created = (await createResp.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_json))!.Item;
        Assert.IsNotNull(created);
        Assert.AreEqual("E2E Task", created.Title);
        var id = created.Id!.Value;

        // READ
        var getResp = await client.GetAsync($"/api/task-items/{id}");
        Assert.AreEqual(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = (await getResp.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_json))!.Item;
        Assert.AreEqual("E2E Task", fetched!.Title);

        // UPDATE
        var updateDto = new TaskItemDto
        {
            Id = id,
            Title = "E2E Task Updated",
            Priority = Priority.Low,
            Status = fetched.Status
        };
        var putResp = await client.PutAsJsonAsync($"/api/task-items/{id}",
            new DefaultRequest<TaskItemDto> { Item = updateDto });
        Assert.AreEqual(HttpStatusCode.OK, putResp.StatusCode,
            $"Update failed: {await putResp.Content.ReadAsStringAsync()}");
        var updated = (await putResp.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_json))!.Item;
        Assert.AreEqual("E2E Task Updated", updated!.Title);

        // DELETE
        var delResp = await client.DeleteAsync($"/api/task-items/{id}");
        Assert.AreEqual(HttpStatusCode.NoContent, delResp.StatusCode);

        // VERIFY DELETED
        var verifyResp = await client.GetAsync($"/api/task-items/{id}");
        Assert.AreEqual(HttpStatusCode.NotFound, verifyResp.StatusCode);
    }

    // ── Category full CRUD ────────────────────────────────────

    [TestMethod]
    public async Task Category_FullCrudCycle_AgainstRealSql()
    {
        using var client = CreateClient();

        var dto = new CategoryDto { Name = "E2E Category", IsActive = true, SortOrder = 1 };
        var createResp = await client.PostAsJsonAsync("/api/categories",
            new DefaultRequest<CategoryDto> { Item = dto });
        Assert.AreEqual(HttpStatusCode.Created, createResp.StatusCode,
            $"Create failed: {await createResp.Content.ReadAsStringAsync()}");
        var created = (await createResp.Content.ReadFromJsonAsync<DefaultResponse<CategoryDto>>(_json))!.Item;
        Assert.IsNotNull(created);
        var id = created.Id!.Value;

        var getResp = await client.GetAsync($"/api/categories/{id}");
        Assert.AreEqual(HttpStatusCode.OK, getResp.StatusCode);

        var updateDto = new CategoryDto { Id = id, Name = "E2E Category Updated", IsActive = true, SortOrder = 2 };
        var putResp = await client.PutAsJsonAsync($"/api/categories/{id}",
            new DefaultRequest<CategoryDto> { Item = updateDto });
        Assert.AreEqual(HttpStatusCode.OK, putResp.StatusCode);

        var delResp = await client.DeleteAsync($"/api/categories/{id}");
        Assert.AreEqual(HttpStatusCode.NoContent, delResp.StatusCode);

        var verifyResp = await client.GetAsync($"/api/categories/{id}");
        Assert.AreEqual(HttpStatusCode.NotFound, verifyResp.StatusCode);
    }

    // ── Tag full CRUD ─────────────────────────────────────────

    [TestMethod]
    public async Task Tag_FullCrudCycle_AgainstRealSql()
    {
        using var client = CreateClient();

        var dto = new TagDto { Name = "e2e-tag", Color = "#FF0000" };
        var createResp = await client.PostAsJsonAsync("/api/tags",
            new DefaultRequest<TagDto> { Item = dto });
        Assert.AreEqual(HttpStatusCode.Created, createResp.StatusCode,
            $"Create failed: {await createResp.Content.ReadAsStringAsync()}");
        var created = (await createResp.Content.ReadFromJsonAsync<DefaultResponse<TagDto>>(_json))!.Item;
        Assert.IsNotNull(created);
        var id = created.Id!.Value;

        var getResp = await client.GetAsync($"/api/tags/{id}");
        Assert.AreEqual(HttpStatusCode.OK, getResp.StatusCode);

        var updateDto = new TagDto { Id = id, Name = "e2e-tag-updated", Color = "#00FF00" };
        var putResp = await client.PutAsJsonAsync($"/api/tags/{id}",
            new DefaultRequest<TagDto> { Item = updateDto });
        Assert.AreEqual(HttpStatusCode.OK, putResp.StatusCode);

        var delResp = await client.DeleteAsync($"/api/tags/{id}");
        Assert.AreEqual(HttpStatusCode.NoContent, delResp.StatusCode);

        var verifyResp = await client.GetAsync($"/api/tags/{id}");
        Assert.AreEqual(HttpStatusCode.NotFound, verifyResp.StatusCode);
    }

    // ── Search works against real SQL ─────────────────────────

    [TestMethod]
    public async Task TaskItem_Search_ReturnsResults_AgainstRealSql()
    {
        using var client = CreateClient();

        // Seed a task
        var searchMarker = $"Searchable E2E {Guid.NewGuid():N}";
        var dto = new TaskItemDto { Title = $"{searchMarker} Task", Priority = Priority.Medium };
        await client.PostAsJsonAsync("/api/task-items",
            new DefaultRequest<TaskItemDto> { Item = dto });

        // Search
        var searchReq = new SearchRequest<TaskItemSearchFilter>
        {
            PageIndex = 1,
            PageSize = 50,
            Filter = new TaskItemSearchFilter { SearchTerm = searchMarker }
        };
        var searchResp = await client.PostAsJsonAsync("/api/task-items/search", searchReq);
        Assert.AreEqual(HttpStatusCode.OK, searchResp.StatusCode);

        using var document = await JsonDocument.ParseAsync(await searchResp.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var total = root.GetProperty("total").GetInt32();
        var titles = root.GetProperty("data")
            .EnumerateArray()
            .Select(item => item.GetProperty("title").GetString())
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Cast<string>()
            .ToList();

        Assert.IsGreaterThanOrEqualTo(total, 1, $"Expected at least 1 search result, got {total}");
        CollectionAssert.Contains(titles, $"{searchMarker} Task");
    }

    [TestMethod]
    public async Task TaskItem_Search_PaginatesDistinctPages_AgainstRealSql()
    {
        using var client = CreateClient();

        var searchMarker = $"Paged Search E2E {Guid.NewGuid():N}";
        foreach (var title in new[] { $"{searchMarker} 01", $"{searchMarker} 02" })
        {
            var dto = new TaskItemDto { Title = title, Priority = Priority.Medium };
            var createResp = await client.PostAsJsonAsync("/api/task-items",
                new DefaultRequest<TaskItemDto> { Item = dto });
            Assert.AreEqual(HttpStatusCode.Created, createResp.StatusCode,
                $"Create failed: {await createResp.Content.ReadAsStringAsync()}");
        }

        async Task<(int Total, List<string> Titles)> SearchPageAsync(int pageIndex)
        {
            var request = new SearchRequest<TaskItemSearchFilter>
            {
                PageIndex = pageIndex,
                PageSize = 1,
                Filter = new TaskItemSearchFilter { SearchTerm = searchMarker }
            };

            var response = await client.PostAsJsonAsync("/api/task-items/search", request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                $"Search failed: {await response.Content.ReadAsStringAsync()}");

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = document.RootElement;
            var total = root.GetProperty("total").GetInt32();
            var titles = root.GetProperty("data")
                .EnumerateArray()
                .Select(item => item.GetProperty("title").GetString())
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Cast<string>()
                .ToList();

            return (total, titles);
        }

        var firstPage = await SearchPageAsync(1);
        var secondPage = await SearchPageAsync(2);

        Assert.AreEqual(2, firstPage.Total);
        Assert.AreEqual(2, secondPage.Total);
        Assert.HasCount(1, firstPage.Titles);
        Assert.HasCount(1, secondPage.Titles);

        var pagedTitles = new[]
        {
            firstPage.Titles[0],
            secondPage.Titles[0]
        };

        CollectionAssert.AreEquivalent(
            new[] { $"{searchMarker} 01", $"{searchMarker} 02" },
            pagedTitles);
    }

    // ── Comment CRUD (child of TaskItem) ──────────────────────

    [TestMethod]
    public async Task Comment_CrudCycle_AgainstRealSql()
    {
        using var client = CreateClient();

        // Create parent task
        var taskDto = new TaskItemDto { Title = "Parent for Comment E2E", Priority = Priority.Low };
        var taskResp = await client.PostAsJsonAsync("/api/task-items",
            new DefaultRequest<TaskItemDto> { Item = taskDto });
        var taskId = (await taskResp.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_json))!.Item!.Id!.Value;

        // Create comment
        var commentDto = new CommentDto { Body = "E2E Comment", TaskItemId = taskId };
        var createResp = await client.PostAsJsonAsync("/api/comments",
            new DefaultRequest<CommentDto> { Item = commentDto });
        Assert.AreEqual(HttpStatusCode.Created, createResp.StatusCode,
            $"Create failed: {await createResp.Content.ReadAsStringAsync()}");
        var created = (await createResp.Content.ReadFromJsonAsync<DefaultResponse<CommentDto>>(_json))!.Item;
        Assert.IsNotNull(created);
        var commentId = created.Id!.Value;

        // Read
        var getResp = await client.GetAsync($"/api/comments/{commentId}");
        Assert.AreEqual(HttpStatusCode.OK, getResp.StatusCode);

        // Delete
        var delResp = await client.DeleteAsync($"/api/comments/{commentId}");
        Assert.AreEqual(HttpStatusCode.NoContent, delResp.StatusCode);
    }

    // ── ChecklistItem CRUD (child of TaskItem) ────────────────

    [TestMethod]
    public async Task ChecklistItem_CrudCycle_AgainstRealSql()
    {
        using var client = CreateClient();

        // Create parent task
        var taskDto = new TaskItemDto { Title = "Parent for Checklist E2E", Priority = Priority.Low };
        var taskResp = await client.PostAsJsonAsync("/api/task-items",
            new DefaultRequest<TaskItemDto> { Item = taskDto });
        var taskId = (await taskResp.Content.ReadFromJsonAsync<DefaultResponse<TaskItemDto>>(_json))!.Item!.Id!.Value;

        // Create checklist item
        var dto = new ChecklistItemDto { Title = "E2E Checklist Item", IsCompleted = false, SortOrder = 1, TaskItemId = taskId };
        var createResp = await client.PostAsJsonAsync("/api/checklist-items",
            new DefaultRequest<ChecklistItemDto> { Item = dto });
        Assert.AreEqual(HttpStatusCode.Created, createResp.StatusCode,
            $"Create failed: {await createResp.Content.ReadAsStringAsync()}");
        var created = (await createResp.Content.ReadFromJsonAsync<DefaultResponse<ChecklistItemDto>>(_json))!.Item;
        Assert.IsNotNull(created);

        // Delete
        var delResp = await client.DeleteAsync($"/api/checklist-items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, delResp.StatusCode);
    }
}
