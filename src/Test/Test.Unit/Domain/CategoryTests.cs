using TaskFlow.Domain.Model;
using Test.Support;

namespace Test.Unit.Domain;

[TestClass]
public class CategoryTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidInput_When_CategoryCreated_Then_ReturnsSuccess()
    {
        var result = Category.Create(TestConstants.TenantId, "Test Category");
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual("Test Category", result.Value.Name);
        Assert.IsTrue(result.Value.IsActive);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Given_EmptyName_When_CategoryCreated_Then_ReturnsDomainFailure(string? name)
    {
        var result = Category.Create(TestConstants.TenantId, name!);
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_EmptyTenantId_When_CategoryCreated_Then_ReturnsDomainFailure()
    {
        var result = Category.Create(Guid.Empty, "Test");
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ExistingCategory_When_Updated_Then_ReturnsUpdatedValues()
    {
        var cat = Category.Create(TestConstants.TenantId, "Original").Value!;
        var result = cat.Update(name: "Updated", description: "New desc");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Updated", result.Value!.Name);
        Assert.AreEqual("New desc", result.Value.Description);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_NullUpdate_When_Updated_Then_OriginalValuesPreserved()
    {
        var cat = Category.Create(TestConstants.TenantId, "Original", "Desc").Value!;
        var result = cat.Update();
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Original", result.Value!.Name);
        Assert.AreEqual("Desc", result.Value.Description);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_CategoryWithParent_When_Created_Then_ParentIdSet()
    {
        var parentId = Guid.NewGuid();
        var result = Category.Create(TestConstants.TenantId, "Child", parentCategoryId: parentId);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(parentId, result.Value!.ParentCategoryId);
    }
}
