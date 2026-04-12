using NetArchTest.Rules;

namespace Test.Architecture;

[TestClass]
[TestCategory("Architecture")]
public class ApplicationDependencyTests : BaseTest
{
    [TestMethod]
    public void Given_ApplicationContractsAssembly_When_DependenciesChecked_Then_NoDependencyOnInfrastructure()
    {
        var result = Types.InAssembly(ApplicationContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TaskFlow.Infrastructure.Data",
                "TaskFlow.Infrastructure.Repositories",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful,
            $"Application.Contracts has forbidden dependency on Infrastructure: {FormatFailingTypes(result)}");
    }

    [TestMethod]
    public void Given_ApplicationContractsAssembly_When_DependenciesChecked_Then_NoDependencyOnHosts()
    {
        var result = Types.InAssembly(ApplicationContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TaskFlow.Api",
                "TaskFlow.Gateway",
                "TaskFlow.Scheduler",
                "TaskFlow.Functions",
                "TaskFlow.Bootstrapper")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful,
            $"Application.Contracts has forbidden dependency on Hosts: {FormatFailingTypes(result)}");
    }

    [TestMethod]
    public void Given_ApplicationServicesAssembly_When_DependenciesChecked_Then_NoDependencyOnInfrastructure()
    {
        var result = Types.InAssembly(ApplicationServicesAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TaskFlow.Infrastructure.Data",
                "TaskFlow.Infrastructure.Repositories",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful,
            $"Application.Services has forbidden dependency on Infrastructure: {FormatFailingTypes(result)}");
    }

    [TestMethod]
    public void Given_ApplicationServicesAssembly_When_DependenciesChecked_Then_NoDependencyOnHosts()
    {
        var result = Types.InAssembly(ApplicationServicesAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TaskFlow.Api",
                "TaskFlow.Gateway",
                "TaskFlow.Scheduler",
                "TaskFlow.Functions",
                "TaskFlow.Bootstrapper")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful,
            $"Application.Services has forbidden dependency on Hosts: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(NetArchTest.Rules.TestResult result) =>
        result.FailingTypes != null
            ? string.Join(", ", result.FailingTypes.Select(t => t.FullName))
            : "none";
}
