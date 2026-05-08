using NetArchTest.Rules;

namespace Test.Architecture;

/// <summary>
/// NetArchTest rules ensuring Infrastructure.Repositories does not depend on Application.Services or any
/// Host project — Infrastructure is allowed to know Application.Contracts but never the implementations
/// or hosts that compose them.
/// Pure-unit tier (NetArchTest only): static assembly checks; no DI, no I/O.
/// </summary>
[TestClass]
[TestCategory("Architecture")]
public class InfrastructureDependencyTests : BaseTest
{
    [TestMethod]
    public void Given_InfrastructureRepositoriesAssembly_When_DependenciesChecked_Then_NoDependencyOnApplicationServices()
    {
        var result = Types.InAssembly(InfrastructureRepositoriesAssembly)
            .ShouldNot()
            .HaveDependencyOn("TaskFlow.Application.Services")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful,
            $"Infrastructure.Repositories has forbidden dependency on Application.Services: {FormatFailingTypes(result)}");
    }

    [TestMethod]
    public void Given_InfrastructureRepositoriesAssembly_When_DependenciesChecked_Then_NoDependencyOnHosts()
    {
        var result = Types.InAssembly(InfrastructureRepositoriesAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TaskFlow.Api",
                "TaskFlow.Gateway",
                "TaskFlow.Scheduler",
                "TaskFlow.Functions",
                "TaskFlow.Bootstrapper")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful,
            $"Infrastructure.Repositories has forbidden dependency on Hosts: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(NetArchTest.Rules.TestResult result) =>
        result.FailingTypes != null
            ? string.Join(", ", result.FailingTypes.Select(t => t.FullName))
            : "none";
}
