using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;
using TaskFlow.Infrastructure.AI.Agents.Tools;
using TaskFlow.Infrastructure.AI.Search;
using Test.Support;

namespace Test.Unit.AI;

[TestClass]
[TestCategory("Unit")]
public class TaskItemToolsTests
{
    private readonly Mock<ITaskItemService> _taskItemServiceMock = new();
    private readonly Mock<ITaskFlowSearchService> _searchServiceMock = new();
    private TaskItemTools _tools = null!;

    [TestInitialize]
    public void Setup()
    {
        _tools = new TaskItemTools(
            NullLogger<TaskItemTools>.Instance,
            _taskItemServiceMock.Object,
            _searchServiceMock.Object);
    }

    [TestMethod]
    public async Task SearchTasks_ReturnsFormattedResults()
    {
        var searchResults = new List<TaskItemSearchResult>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Fix login bug",
                Status = "Open",
                Priority = "High",
                DueDate = DateTimeOffset.UtcNow.AddDays(3)
            }
        };

        _searchServiceMock
            .Setup(x => x.SearchTaskItemsAsync("login", SearchMode.Hybrid, null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        var result = await _tools.SearchTasks("login");

        Assert.IsTrue(result.Contains("Fix login bug"));
        Assert.IsTrue(result.Contains("High"));
    }

    [TestMethod]
    public async Task SearchTasks_NoResults_ReturnsNotFoundMessage()
    {
        _searchServiceMock
            .Setup(x => x.SearchTaskItemsAsync(It.IsAny<string>(), It.IsAny<SearchMode>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskItemSearchResult>());

        var result = await _tools.SearchTasks("nonexistent");

        Assert.AreEqual("No tasks found matching your query.", result);
    }

    [TestMethod]
    public async Task GetTaskDetails_ReturnsTaskInfo()
    {
        var taskId = Guid.NewGuid();
        var dto = new TaskItemDto
        {
            Id = taskId,
            Title = "Important Task",
            Status = TaskItemStatus.InProgress,
            Priority = Priority.High,
            Description = "Do important things"
        };

        _taskItemServiceMock
            .Setup(x => x.GetAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DefaultResponse<TaskItemDto>>.Success(new DefaultResponse<TaskItemDto> { Item = dto }));

        var result = await _tools.GetTaskDetails(taskId.ToString());

        Assert.IsTrue(result.Contains("Important Task"));
        Assert.IsTrue(result.Contains("InProgress"));
    }

    [TestMethod]
    public async Task GetTaskDetails_InvalidId_ReturnsError()
    {
        var result = await _tools.GetTaskDetails("not-a-guid");

        Assert.IsTrue(result.Contains("Invalid task ID"));
    }

    [TestMethod]
    public async Task GetTaskDetails_NotFound_ReturnsNotFound()
    {
        var taskId = Guid.NewGuid();

        _taskItemServiceMock
            .Setup(x => x.GetAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DefaultResponse<TaskItemDto>>.None());

        var result = await _tools.GetTaskDetails(taskId.ToString());

        Assert.IsTrue(result.Contains("not found"));
    }

    [TestMethod]
    public async Task CreateTask_CallsService_ReturnsConfirmation()
    {
        var newId = Guid.NewGuid();
        _taskItemServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<DefaultRequest<TaskItemDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DefaultResponse<TaskItemDto>>.Success(
                new DefaultResponse<TaskItemDto> { Item = new TaskItemDto { Id = newId, Title = "New task" } }));

        var result = await _tools.CreateTask("New task", "A description", "High");

        Assert.IsTrue(result.Contains("Created task"));
        Assert.IsTrue(result.Contains(newId.ToString()));
    }

    [TestMethod]
    public async Task SummarizeBacklog_ReturnsStatusBreakdown()
    {
        var tasks = new List<TaskItemDto>
        {
            new() { Title = "T1", Status = TaskItemStatus.Open },
            new() { Title = "T2", Status = TaskItemStatus.Open },
            new() { Title = "T3", Status = TaskItemStatus.InProgress },
            new() { Title = "T4", Status = TaskItemStatus.Completed }
        };

        _taskItemServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<SearchRequest<TaskItemSearchFilter>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<TaskItemDto> { Data = tasks, Total = 4, PageSize = 100, PageIndex = 0 });

        var result = await _tools.SummarizeBacklog();

        Assert.IsTrue(result.Contains("4 total"));
        Assert.IsTrue(result.Contains("Open: 2"));
        Assert.IsTrue(result.Contains("InProgress: 1"));
    }
}
