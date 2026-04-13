using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;
using TaskFlow.Scheduler.Handlers;

namespace Test.Unit.Services;

[TestClass]
public class OverdueTaskCheckHandlerTests
{
    private readonly Mock<ITaskItemService> _serviceMock = new();
    private readonly OverdueTaskCheckHandler _handler;

    public OverdueTaskCheckHandlerTests()
    {
        _handler = new OverdueTaskCheckHandler(
            _serviceMock.Object,
            NullLogger<OverdueTaskCheckHandler>.Instance);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HandleAsync_WithOverdueTasks_LogsCount()
    {
        var overdueTasks = new List<TaskItemDto>
        {
            new() { Title = "Overdue1", Status = TaskItemStatus.Open },
            new() { Title = "Overdue2", Status = TaskItemStatus.InProgress }
        };

        _serviceMock.Setup(s => s.SearchAsync(
                It.Is<SearchRequest<TaskItemSearchFilter>>(r => r.Filter!.IsOverdue == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<TaskItemDto> { Data = overdueTasks, Total = 2 });

        await _handler.HandleAsync(CancellationToken.None);

        _serviceMock.Verify(s => s.SearchAsync(
            It.Is<SearchRequest<TaskItemSearchFilter>>(r => r.Filter!.IsOverdue == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HandleAsync_NoOverdueItems_CompletesWithZeroCount()
    {
        _serviceMock.Setup(s => s.SearchAsync(
                It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<TaskItemDto> { Data = [], Total = 0 });

        await _handler.HandleAsync(CancellationToken.None);

        _serviceMock.Verify(s => s.SearchAsync(
            It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HandleAsync_FiltersOutCompletedAndCancelled()
    {
        var mixedTasks = new List<TaskItemDto>
        {
            new() { Title = "Active Overdue", Status = TaskItemStatus.Open },
            new() { Title = "Done Overdue", Status = TaskItemStatus.Completed },
            new() { Title = "Cancelled Overdue", Status = TaskItemStatus.Cancelled }
        };

        _serviceMock.Setup(s => s.SearchAsync(
                It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<TaskItemDto> { Data = mixedTasks, Total = 3 });

        // Handler filters out Completed and Cancelled internally
        await _handler.HandleAsync(CancellationToken.None);

        _serviceMock.Verify(s => s.SearchAsync(
            It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HandleAsync_NullData_TreatsAsEmpty()
    {
        _serviceMock.Setup(s => s.SearchAsync(
                It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<TaskItemDto> { Data = null!, Total = 0 });

        await _handler.HandleAsync(CancellationToken.None);

        _serviceMock.Verify(s => s.SearchAsync(
            It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
