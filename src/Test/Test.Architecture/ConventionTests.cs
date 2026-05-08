using System.Reflection;
using EF.Domain.Contracts;

namespace Test.Architecture;

/// <summary>
/// Reflection-based convention checks: every domain entity implements <c>ITenantEntity&lt;Guid&gt;</c>,
/// every <c>*Service</c> implementation has a matching <c>I*Service</c> interface, and entity properties
/// (excluding <c>TenantId</c>) have private setters to enforce encapsulation.
/// Pure-unit tier (NetArchTest/reflection only): inspects loaded type metadata; no runtime, no infra.
/// </summary>
[TestClass]
[TestCategory("Architecture")]
public class ConventionTests : BaseTest
{
    private static readonly Type[] KnownEntities =
    [
        typeof(TaskFlow.Domain.Model.Category),
        typeof(TaskFlow.Domain.Model.Tag),
        typeof(TaskFlow.Domain.Model.TaskItem),
        typeof(TaskFlow.Domain.Model.Comment),
        typeof(TaskFlow.Domain.Model.ChecklistItem),
        typeof(TaskFlow.Domain.Model.Attachment),
        typeof(TaskFlow.Domain.Model.TaskItemTag)
    ];

    [TestMethod]
    public void Given_DomainEntities_When_Checked_Then_AllImplementITenantEntity()
    {
        var tenantInterface = typeof(ITenantEntity<Guid>);
        var nonTenantEntities = KnownEntities
            .Where(e => !tenantInterface.IsAssignableFrom(e))
            .ToList();

        Assert.IsEmpty(nonTenantEntities,
            $"Entities missing ITenantEntity<Guid>: {string.Join(", ", nonTenantEntities.Select(t => t.Name))}");
    }

    [TestMethod]
    public void Given_ServiceImplementations_When_Checked_Then_AllImplementTheirInterface()
    {
        var serviceTypes = ApplicationServicesAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service"))
            .ToList();

        Assert.IsNotEmpty(serviceTypes, "No service implementations found");

        var failures = new List<string>();
        foreach (var service in serviceTypes)
        {
            var expectedInterface = $"I{service.Name}";
            var implementsInterface = service.GetInterfaces()
                .Any(i => i.Name == expectedInterface);

            if (!implementsInterface)
                failures.Add($"{service.Name} does not implement {expectedInterface}");
        }

        Assert.IsEmpty(failures,
            $"Service convention violations: {string.Join("; ", failures)}");
    }

    [TestMethod]
    public void Given_DomainEntities_When_Checked_Then_AllHavePrivateSetters()
    {
        // TenantId is excluded — public setter required by ITenantEntity<Guid> interface contract
        var excludedProperties = new HashSet<string> { "TenantId" };

        var violations = new List<string>();
        foreach (var entity in KnownEntities)
        {
            var publicSetters = entity.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.SetMethod is { IsPublic: true } && !excludedProperties.Contains(p.Name))
                .ToList();

            if (publicSetters.Count > 0)
                violations.Add($"{entity.Name}: {string.Join(", ", publicSetters.Select(p => p.Name))}");
        }

        Assert.IsEmpty(violations,
            $"Entities with public setters (encapsulation violation): {string.Join("; ", violations)}");
    }
}
