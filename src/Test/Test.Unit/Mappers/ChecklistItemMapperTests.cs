using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Mappers;

/// <summary>
/// Validates <c>ChecklistItemMapper</c> entity ↔ DTO mapping including the parent <c>TaskItemId</c>
/// linkage and failure surfacing for invalid DTO input.
/// Pure-unit tier: static mapping extensions only.
/// </summary>
[TestClass]
public class ChecklistItemMapperTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidEntity_When_MappedToDto_Then_AllPropertiesMapped()
    {
        var taskItemId = Guid.NewGuid();
        var entity = new ChecklistItemBuilder().WithTaskItemId(taskItemId).WithSortOrder(2).Build();
        var dto = entity.ToDto();

        Assert.AreEqual(entity.Id, dto.Id);
        Assert.AreEqual(entity.Title, dto.Title);
        Assert.AreEqual(entity.IsCompleted, dto.IsCompleted);
        Assert.AreEqual(entity.SortOrder, dto.SortOrder);
        Assert.AreEqual(entity.CompletedDate, dto.CompletedDate);
        Assert.AreEqual(entity.TaskItemId, dto.TaskItemId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidDto_When_MappedToEntity_Then_ReturnsSuccessDomainResult()
    {
        var taskItemId = Guid.NewGuid();
        var dto = new ChecklistItemDto { Title = "Step 1", TaskItemId = taskItemId, SortOrder = 1 };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Step 1", result.Value!.Title);
        Assert.AreEqual(taskItemId, result.Value.TaskItemId);
        Assert.AreEqual(1, result.Value.SortOrder);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_InvalidDto_When_MappedToEntity_Then_ReturnsFailure()
    {
        var dto = new ChecklistItemDto { Title = "", TaskItemId = Guid.Empty };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsFailure);
    }
}
