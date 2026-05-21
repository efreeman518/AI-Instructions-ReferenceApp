using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Mappers;

/// <summary>
/// Validates <c>TaskItemTagMapper</c> entity ↔ DTO mapping for the bridge type and failure surfacing
/// for empty foreign keys.
/// Pure-unit tier: static mapping extensions only.
/// </summary>
[TestClass]
public class TaskItemTagMapperTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidEntity_When_MappedToDto_Then_AllPropertiesMapped()
    {
        var taskItemId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var entity = new TaskItemTagBuilder().WithTaskItemId(taskItemId).WithTagId(tagId).Build();
        var dto = entity.ToDto();

        Assert.AreEqual(entity.Id, dto.Id);
        Assert.AreEqual(entity.TaskItemId, dto.TaskItemId);
        Assert.AreEqual(entity.TagId, dto.TagId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidDto_When_MappedToEntity_Then_ReturnsSuccessDomainResult()
    {
        var taskItemId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var dto = new TaskItemTagDto { TaskItemId = taskItemId, TagId = tagId };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(taskItemId, result.Value!.TaskItemId);
        Assert.AreEqual(tagId, result.Value.TagId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_InvalidDto_When_MappedToEntity_Then_ReturnsFailure()
    {
        var dto = new TaskItemTagDto { TaskItemId = Guid.Empty, TagId = Guid.Empty };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsFailure);
    }
}
