using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using Test.Support;
using Test.Support.Builders;

namespace Test.Unit.Mappers;

[TestClass]
public class TagMapperTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidEntity_When_MappedToDto_Then_AllPropertiesMapped()
    {
        var entity = new TagBuilder().WithColor("#00FF00").Build();
        var dto = entity.ToDto();

        Assert.AreEqual(entity.Id, dto.Id);
        Assert.AreEqual(entity.Name, dto.Name);
        Assert.AreEqual(entity.Color, dto.Color);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_ValidDto_When_MappedToEntity_Then_ReturnsSuccessDomainResult()
    {
        var dto = new TagDto { Name = "Urgent", Color = "#FF0000" };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Urgent", result.Value!.Name);
        Assert.AreEqual("#FF0000", result.Value.Color);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_InvalidDto_When_MappedToEntity_Then_ReturnsFailure()
    {
        var dto = new TagDto { Name = "" };
        var result = dto.ToEntity(TestConstants.TenantId);

        Assert.IsTrue(result.IsFailure);
    }
}
