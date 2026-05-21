using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Mappers;

/// <summary>
/// Validates <c>CategoryMapper</c> entity ↔ DTO round-trips: every property is preserved,
/// <c>ParentCategoryId</c> survives mapping, and invalid DTO input is surfaced as a <c>DomainResult</c>
/// failure rather than throwing.
/// Pure-unit tier: static extension methods over POCOs — no DbContext, no DI.
/// </summary>
[TestClass]
public class CategoryMapperTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidEntity_When_MappedToDto_Then_AllPropertiesMapped()
    {
        var entity = new CategoryBuilder().WithSortOrder(3).Build();
        var dto = entity.ToDto();

        Assert.AreEqual(entity.Id, dto.Id);
        Assert.AreEqual(entity.Name, dto.Name);
        Assert.AreEqual(entity.Description, dto.Description);
        Assert.AreEqual(entity.SortOrder, dto.SortOrder);
        Assert.AreEqual(entity.IsActive, dto.IsActive);
        Assert.AreEqual(entity.ParentCategoryId, dto.ParentCategoryId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidDto_When_MappedToEntity_Then_ReturnsSuccessDomainResult()
    {
        var dto = new CategoryDto { Name = "From DTO", Description = "Test desc", SortOrder = 2 };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("From DTO", result.Value!.Name);
        Assert.AreEqual("Test desc", result.Value.Description);
        Assert.AreEqual(2, result.Value.SortOrder);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_DtoWithParentCategoryId_When_MappedToEntity_Then_ParentCategoryIdPreserved()
    {
        var parentId = Guid.NewGuid();
        var dto = new CategoryDto { Name = "Child", ParentCategoryId = parentId };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(parentId, result.Value!.ParentCategoryId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_InvalidDto_When_MappedToEntity_Then_ReturnsFailure()
    {
        var dto = new CategoryDto { Name = "" };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsFailure);
    }
}
