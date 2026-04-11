using TaskFlow.Domain.Model;
using Test.Support;

namespace Test.Unit.Domain;

[TestClass]
public class TagTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidInput_When_TagCreated_Then_ReturnsSuccess()
    {
        var result = Tag.Create(TestConstants.TenantId, "Test Tag", "#FF0000");
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual("Test Tag", result.Value.Name);
        Assert.AreEqual("#FF0000", result.Value.Color);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Given_EmptyName_When_TagCreated_Then_ReturnsDomainFailure(string? name)
    {
        var result = Tag.Create(TestConstants.TenantId, name!);
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_EmptyTenantId_When_TagCreated_Then_ReturnsDomainFailure()
    {
        var result = Tag.Create(Guid.Empty, "Test");
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ExistingTag_When_Updated_Then_ReturnsUpdatedValues()
    {
        var tag = Tag.Create(TestConstants.TenantId, "Original", "#000000").Value!;
        var result = tag.Update(name: "Updated", color: "#FFFFFF");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Updated", result.Value!.Name);
        Assert.AreEqual("#FFFFFF", result.Value.Color);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_NullUpdate_When_Updated_Then_OriginalValuesPreserved()
    {
        var tag = Tag.Create(TestConstants.TenantId, "Original", "#000000").Value!;
        var result = tag.Update();
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Original", result.Value!.Name);
        Assert.AreEqual("#000000", result.Value.Color);
    }
}
