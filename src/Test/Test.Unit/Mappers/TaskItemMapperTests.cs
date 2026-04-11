using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Enums;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Mappers;

[TestClass]
public class TaskItemMapperTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidEntity_When_MappedToDto_Then_AllPropertiesMapped()
    {
        var entity = new TaskItemBuilder().WithPriority(Priority.High).Build();
        var dto = entity.ToDto();

        Assert.AreEqual(entity.Id, dto.Id);
        Assert.AreEqual(entity.Title, dto.Title);
        Assert.AreEqual(entity.Description, dto.Description);
        Assert.AreEqual(entity.Priority, dto.Priority);
        Assert.AreEqual(entity.Status, dto.Status);
        Assert.AreEqual(entity.CategoryId, dto.CategoryId);
        Assert.AreEqual(entity.ParentTaskItemId, dto.ParentTaskItemId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_EntityWithDateRange_When_MappedToDto_Then_DatesFlattenedCorrectly()
    {
        var entity = new TaskItemBuilder().Build();
        var start = DateTimeOffset.UtcNow;
        var due = start.AddDays(7);
        entity.UpdateDateRange(start, due);

        var dto = entity.ToDto();

        Assert.AreEqual(start, dto.StartDate);
        Assert.AreEqual(due, dto.DueDate);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidDto_When_MappedToEntity_Then_ReturnsSuccessDomainResult()
    {
        var dto = new TaskItemDto { Title = "New Task", Priority = Priority.Medium };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("New Task", result.Value!.Title);
        Assert.AreEqual(Priority.Medium, result.Value.Priority);
        Assert.AreEqual(TaskItemStatus.Open, result.Value.Status);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_DtoWithDates_When_MappedToEntity_Then_DateRangeSet()
    {
        var start = DateTimeOffset.UtcNow;
        var due = start.AddDays(5);
        var dto = new TaskItemDto { Title = "With Dates", StartDate = start, DueDate = due };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(start, result.Value!.DateRange.StartDate);
        Assert.AreEqual(due, result.Value.DateRange.DueDate);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_DtoWithRecurrence_When_MappedToEntity_Then_RecurrencePatternSet()
    {
        var dto = new TaskItemDto
        {
            Title = "Recurring Task",
            RecurrenceInterval = 1,
            RecurrenceFrequency = "Weekly",
            RecurrenceEndDate = DateTimeOffset.UtcNow.AddMonths(3)
        };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value!.RecurrencePattern);
        Assert.AreEqual(1, result.Value.RecurrencePattern!.Interval);
        Assert.AreEqual("Weekly", result.Value.RecurrencePattern.Frequency);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_InvalidDto_When_MappedToEntity_Then_ReturnsFailure()
    {
        var dto = new TaskItemDto { Title = "" };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsFailure);
    }
}
