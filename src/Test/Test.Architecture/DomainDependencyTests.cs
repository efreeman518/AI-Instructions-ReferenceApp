using NetArchTest.Rules;

namespace Test.Architecture;

/// <summary>
/// NetArchTest rules pinning the Domain.Model assembly as a leaf dependency: it must not reference
/// Application (Contracts/Services/Mappers/Models/MessageHandlers), Infrastructure, EF Core, or any
/// Host project.
/// Pure-unit tier (NetArchTest only): assembly-metadata inspection, no infra. Anything heavier is wasted —
/// these are pure dependency-direction assertions.
/// </summary>
[TestClass]
[TestCategory("Architecture")]
public class DomainDependencyTests : BaseTest
{
    [TestMethod]
    public void Given_DomainModelAssembly_When_DependenciesChecked_Then_NoDependencyOnApplication()
    {
        var result = Types.InAssembly(DomainModelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TaskFlow.Application.Contracts",
                "TaskFlow.Application.Services",
                "TaskFlow.Application.Mappers",
                "TaskFlow.Application.Models",
                "TaskFlow.Application.MessageHandlers")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful,
            $"Domain.Model has forbidden dependency on Application: {FormatFailingTypes(result)}");
    }

    [TestMethod]
    public void Given_DomainModelAssembly_When_DependenciesChecked_Then_NoDependencyOnInfrastructure()
    {
        var result = Types.InAssembly(DomainModelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TaskFlow.Infrastructure.Data",
                "TaskFlow.Infrastructure.Repositories",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful,
            $"Domain.Model has forbidden dependency on Infrastructure: {FormatFailingTypes(result)}");
    }

    [TestMethod]
    public void Given_DomainModelAssembly_When_DependenciesChecked_Then_NoDependencyOnHosts()
    {
        var result = Types.InAssembly(DomainModelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TaskFlow.Api",
                "TaskFlow.Gateway",
                "TaskFlow.Scheduler",
                "TaskFlow.Functions",
                "TaskFlow.Bootstrapper")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful,
            $"Domain.Model has forbidden dependency on Hosts: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(NetArchTest.Rules.TestResult result) =>
        result.FailingTypes != null
            ? string.Join(", ", result.FailingTypes.Select(t => t.FullName))
            : "none";
}
