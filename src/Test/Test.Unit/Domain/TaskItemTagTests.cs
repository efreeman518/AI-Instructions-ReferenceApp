using TaskFlow.Domain.Model;
using Test.Support;

namespace Test.Unit.Domain;

[TestClass]
public class TaskItemTagTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidInput_When_TaskItemTagCreated_Then_ReturnsSuccess()
    {
        var taskItemId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var result = TaskItemTag.Create(TestConstants.TenantId, taskItemId, tagId);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(taskItemId, result.Value.TaskItemId);
        Assert.AreEqual(tagId, result.Value.TagId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_EmptyTaskItemId_When_TaskItemTagCreated_Then_ReturnsDomainFailure()
    {
        var result = TaskItemTag.Create(TestConstants.TenantId, Guid.Empty, Guid.NewGuid());
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_EmptyTagId_When_TaskItemTagCreated_Then_ReturnsDomainFailure()
    {
        var result = TaskItemTag.Create(TestConstants.TenantId, Guid.NewGuid(), Guid.Empty);
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_EmptyTenantId_When_TaskItemTagCreated_Then_ReturnsDomainFailure()
    {
        var result = TaskItemTag.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        Assert.IsTrue(result.IsFailure);
    }
}
