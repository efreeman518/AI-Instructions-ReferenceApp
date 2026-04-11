using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Mappers;

[TestClass]
public class CommentMapperTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidEntity_When_MappedToDto_Then_AllPropertiesMapped()
    {
        var taskItemId = Guid.NewGuid();
        var entity = new CommentBuilder().WithTaskItemId(taskItemId).WithBody("Test body").Build();
        var dto = entity.ToDto();

        Assert.AreEqual(entity.Id, dto.Id);
        Assert.AreEqual(entity.Body, dto.Body);
        Assert.AreEqual(entity.TaskItemId, dto.TaskItemId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidDto_When_MappedToEntity_Then_ReturnsSuccessDomainResult()
    {
        var taskItemId = Guid.NewGuid();
        var dto = new CommentDto { Body = "A comment", TaskItemId = taskItemId };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("A comment", result.Value!.Body);
        Assert.AreEqual(taskItemId, result.Value.TaskItemId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_InvalidDto_When_MappedToEntity_Then_ReturnsFailure()
    {
        var dto = new CommentDto { Body = "", TaskItemId = Guid.Empty };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsFailure);
    }
}
