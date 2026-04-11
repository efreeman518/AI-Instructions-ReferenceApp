using TaskFlow.Domain.Model;
using Test.Support;

namespace Test.Unit.Domain;

[TestClass]
public class ChecklistItemTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidInput_When_ChecklistItemCreated_Then_ReturnsSuccess()
    {
        var taskItemId = Guid.NewGuid();
        var result = ChecklistItem.Create(TestConstants.TenantId, taskItemId, "Test Item", 1);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual("Test Item", result.Value.Title);
        Assert.IsFalse(result.Value.IsCompleted);
        Assert.IsNull(result.Value.CompletedDate);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Given_EmptyTitle_When_ChecklistItemCreated_Then_ReturnsDomainFailure(string? title)
    {
        var result = ChecklistItem.Create(TestConstants.TenantId, Guid.NewGuid(), title!);
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_EmptyTaskItemId_When_ChecklistItemCreated_Then_ReturnsDomainFailure()
    {
        var result = ChecklistItem.Create(TestConstants.TenantId, Guid.Empty, "Title");
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ExistingItem_When_UpdatedWithTitle_Then_ReturnsUpdatedValues()
    {
        var item = ChecklistItem.Create(TestConstants.TenantId, Guid.NewGuid(), "Original").Value!;
        var result = item.Update(title: "Updated");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Updated", result.Value!.Title);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_IncompleteItem_When_CompletionSet_Then_CompletedDateSet()
    {
        var item = ChecklistItem.Create(TestConstants.TenantId, Guid.NewGuid(), "Item").Value!;
        var result = item.Update(isCompleted: true);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Value!.IsCompleted);
        Assert.IsNotNull(result.Value.CompletedDate);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_CompletedItem_When_Uncompleted_Then_CompletedDateCleared()
    {
        var item = ChecklistItem.Create(TestConstants.TenantId, Guid.NewGuid(), "Item").Value!;
        item.Update(isCompleted: true);
        Assert.IsNotNull(item.CompletedDate);

        var result = item.Update(isCompleted: false);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.Value!.IsCompleted);
        Assert.IsNull(result.Value.CompletedDate);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_NullUpdate_When_Updated_Then_OriginalValuesPreserved()
    {
        var item = ChecklistItem.Create(TestConstants.TenantId, Guid.NewGuid(), "Original", 5).Value!;
        var result = item.Update();
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Original", result.Value!.Title);
        Assert.AreEqual(5, result.Value.SortOrder);
    }
}
