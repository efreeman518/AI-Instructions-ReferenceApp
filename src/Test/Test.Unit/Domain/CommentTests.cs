using TaskFlow.Domain.Model;
using Test.Support;

namespace Test.Unit.Domain;

/// <summary>
/// Validates the <see cref="TaskFlow.Domain.Model.Comment"/> aggregate's factory and update rules:
/// required body and TaskItemId, and null-update preservation of the original body.
/// Pure-unit tier: POCO behavior only.
/// </summary>
[TestClass]
public class CommentTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidInput_When_CommentCreated_Then_ReturnsSuccess()
    {
        var taskItemId = Guid.NewGuid();
        var result = Comment.Create(TestConstants.TenantId, taskItemId, "Test comment body");
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual("Test comment body", result.Value.Body);
        Assert.AreEqual(taskItemId, result.Value.TaskItemId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Given_EmptyBody_When_CommentCreated_Then_ReturnsDomainFailure(string? body)
    {
        var result = Comment.Create(TestConstants.TenantId, Guid.NewGuid(), body!);
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_EmptyTaskItemId_When_CommentCreated_Then_ReturnsDomainFailure()
    {
        var result = Comment.Create(TestConstants.TenantId, Guid.Empty, "Body");
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ExistingComment_When_Updated_Then_ReturnsUpdatedValues()
    {
        var comment = Comment.Create(TestConstants.TenantId, Guid.NewGuid(), "Original").Value!;
        var result = comment.Update(body: "Updated body");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Updated body", result.Value!.Body);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_NullUpdate_When_Updated_Then_OriginalValuesPreserved()
    {
        var comment = Comment.Create(TestConstants.TenantId, Guid.NewGuid(), "Original").Value!;
        var result = comment.Update();
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Original", result.Value!.Body);
    }
}
