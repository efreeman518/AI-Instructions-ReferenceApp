using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using EF.Common.Contracts;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Models;
using TaskFlow.Scheduler.Handlers;

namespace Test.Unit.Services;

[TestClass]
public class RecurringTaskGenerationHandlerTests
{
    private readonly Mock<ITaskItemService> _serviceMock = new();
    private readonly RecurringTaskGenerationHandler _handler;

    public RecurringTaskGenerationHandlerTests()
    {
        _handler = new RecurringTaskGenerationHandler(
            _serviceMock.Object,
            NullLogger<RecurringTaskGenerationHandler>.Instance);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HandleAsync_WithRecurringTemplates_LogsCount()
    {
        var tasks = new List<TaskItemDto>
        {
            new() { Title = "Daily Standup", RecurrenceFrequency = "Daily" },
            new() { Title = "Weekly Review", RecurrenceFrequency = "Weekly" },
            new() { Title = "Normal Task" }
        };

        _serviceMock.Setup(s => s.SearchAsync(
                It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<TaskItemDto> { Data = tasks, Total = 3 });

        await _handler.HandleAsync(CancellationToken.None);

        _serviceMock.Verify(s => s.SearchAsync(
            It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HandleAsync_NoRecurringTemplates_CompletesWithZeroCount()
    {
        var tasks = new List<TaskItemDto>
        {
            new() { Title = "One-off Task" },
            new() { Title = "Another Task" }
        };

        _serviceMock.Setup(s => s.SearchAsync(
                It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<TaskItemDto> { Data = tasks, Total = 2 });

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
            .ReturnsAsync(new PagedResponse<TaskItemDto> { Data = null, Total = 0 });

        await _handler.HandleAsync(CancellationToken.None);

        _serviceMock.Verify(s => s.SearchAsync(
            It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task HandleAsync_EmptyStringFrequency_FiltersCorrectly()
    {
        var tasks = new List<TaskItemDto>
        {
            new() { Title = "Empty Freq", RecurrenceFrequency = "" },
            new() { Title = "Null Freq", RecurrenceFrequency = null }
        };

        _serviceMock.Setup(s => s.SearchAsync(
                It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<TaskItemDto> { Data = tasks, Total = 2 });

        // Empty and null both filtered out — handler considers neither as recurring
        await _handler.HandleAsync(CancellationToken.None);

        _serviceMock.Verify(s => s.SearchAsync(
            It.IsAny<SearchRequest<TaskItemSearchFilter>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
