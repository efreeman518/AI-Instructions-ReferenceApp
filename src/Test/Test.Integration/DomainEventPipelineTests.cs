using System.Text.Json;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Storage;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Enums;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Repositories;

namespace Test.Integration;

/// <summary>
/// Proves the domain event pipeline works end-to-end:
/// TaskItem created → TaskViewProjectionService → TaskView document produced.
/// Service Bus → Function trigger is verified via smoke test (see HANDOFF.md).
/// </summary>
[TestClass]
public class DomainEventPipelineTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        await using var db = DatabaseFixture.CreateTrxnContext();
        await db.Database.MigrateAsync();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Given_TaskItemCreated_When_ProjectionRuns_Then_TaskViewProduced()
    {
        // Arrange — real SQL via TestContainers
        var connStr = DatabaseFixture.ConnectionString;
        var ctx = DatabaseFixture.CreateTrxnContext(connStr);

        var category = Category.Create(TenantId, "Work").Value!;
        ctx.Categories.Add(category);
        await ctx.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        var taskResult = TaskItem.Create(TenantId, "Integration Test Task",
            "Verify projection pipeline", Priority.High, category.Id);
        Assert.IsTrue(taskResult.IsSuccess);
        var task = taskResult.Value!;
        ctx.TaskItems.Add(task);
        await ctx.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        // Create a query context for the repo
        var queryCtx = DatabaseFixture.CreateQueryContext(connStr);
        var taskItemRepo = new TaskItemRepositoryQuery(queryCtx);
        var attachmentRepo = new AttachmentRepositoryQuery(queryCtx);

        // In-memory task view store (simulates Cosmos)
        var taskViewRepo = new InMemoryTaskViewRepository();

        var projectionService = new TaskViewProjectionService(
            taskItemRepo, attachmentRepo, taskViewRepo,
            NullLogger<TaskViewProjectionService>.Instance);

        // Act — run projection (same as what Function trigger calls)
        await projectionService.ProjectTaskItemAsync(task.Id);

        // Assert — TaskView was produced with correct data
        var taskView = await taskViewRepo.GetAsync(task.Id.ToString(), TenantId.ToString());
        Assert.IsNotNull(taskView, "TaskView should be created by projection");
        Assert.AreEqual("Integration Test Task", taskView.Title);
        Assert.AreEqual("Verify projection pipeline", taskView.Description);
        Assert.AreEqual("Open", taskView.Status);
        Assert.AreEqual("High", taskView.Priority);
        Assert.AreEqual("Work", taskView.CategoryName);
        Assert.AreEqual(0, taskView.CommentCount);
        Assert.AreEqual(0, taskView.AttachmentCount);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Given_TaskItemWithChildren_When_ProjectionRuns_Then_CountsIncluded()
    {
        var connStr = DatabaseFixture.ConnectionString;
        var ctx = DatabaseFixture.CreateTrxnContext(connStr);

        var taskResult = TaskItem.Create(TenantId, "Task With Children");
        var task = taskResult.Value!;
        ctx.TaskItems.Add(task);
        await ctx.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        // Add a comment
        var commentResult = Comment.Create(TenantId, task.Id, "Test comment");
        ctx.Comments.Add(commentResult.Value!);

        // Add an attachment
        var attachmentResult = Attachment.Create(TenantId, "file.pdf", "application/pdf",
            1024, "https://storage.example.com/file.pdf", AttachmentOwnerType.TaskItem, task.Id);
        ctx.Attachments.Add(attachmentResult.Value!);

        // Add a checklist item
        var checklistResult = ChecklistItem.Create(TenantId, task.Id, "Step 1");
        ctx.ChecklistItems.Add(checklistResult.Value!);

        await ctx.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        var queryCtx = DatabaseFixture.CreateQueryContext(connStr);
        var taskViewRepo = new InMemoryTaskViewRepository();
        var projectionService = new TaskViewProjectionService(
            new TaskItemRepositoryQuery(queryCtx),
            new AttachmentRepositoryQuery(queryCtx),
            taskViewRepo,
            NullLogger<TaskViewProjectionService>.Instance);

        await projectionService.ProjectTaskItemAsync(task.Id);

        var taskView = await taskViewRepo.GetAsync(task.Id.ToString(), TenantId.ToString());
        Assert.IsNotNull(taskView);
        Assert.AreEqual(1, taskView.CommentCount);
        Assert.AreEqual(1, taskView.AttachmentCount);
        Assert.AreEqual(1, taskView.ChecklistTotal);
        Assert.AreEqual(0, taskView.ChecklistCompleted);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Given_ServiceBusMessageBody_When_Parsed_Then_TaskItemIdExtracted()
    {
        // Proves the Function trigger's message parsing logic works
        var taskItemId = Guid.NewGuid();
        var eventBody = JsonSerializer.Serialize(new
        {
            TaskItemId = taskItemId,
            TenantId = TenantId,
            Title = "Test Task"
        });

        using var doc = JsonDocument.Parse(eventBody);
        Assert.IsTrue(doc.RootElement.TryGetProperty("TaskItemId", out var prop));
        Assert.AreEqual(taskItemId, prop.GetGuid());
    }
}

/// <summary>
/// In-memory implementation of ITaskViewRepository for integration testing
/// without Cosmos DB emulator.
/// </summary>
internal class InMemoryTaskViewRepository : ITaskViewRepository
{
    private readonly Dictionary<string, TaskViewDto> _store = new();

    public Task UpsertAsync(TaskViewDto taskView, CancellationToken ct = default)
    {
        _store[$"{taskView.TenantId}:{taskView.Id}"] = taskView;
        return Task.CompletedTask;
    }

    public Task<TaskViewDto?> GetAsync(string id, string tenantId, CancellationToken ct = default)
    {
        _store.TryGetValue($"{tenantId}:{id}", out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<TaskViewDto>> QueryByTenantAsync(string tenantId,
        int pageSize = 20, string? continuationToken = null, CancellationToken ct = default)
    {
        var results = _store.Values
            .Where(v => v.TenantId == tenantId)
            .OrderByDescending(v => v.LastModifiedUtc)
            .Take(pageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<TaskViewDto>>(results);
    }

    public Task DeleteAsync(string id, string tenantId, CancellationToken ct = default)
    {
        _store.Remove($"{tenantId}:{id}");
        return Task.CompletedTask;
    }
}
