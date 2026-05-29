using EF.CQRS.Abstractions;
using TaskFlow.Application.Cqrs.Registration;

namespace Test.Architecture;

/// <summary>Covers CQRS architecture behavior with focused assertions that document expected behavior and regression intent.</summary>
[TestClass]
public sealed class CqrsArchitectureTests : BaseTest
{
    /// <summary>Verifies EF CQRS package does not depend on task flow or mediat r behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void EfCqrsPackage_DoesNotDependOn_TaskFlowOrMediatR()
    {
        var referenced = EfCqrsAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        var forbiddenReferences = referenced
            .Where(n => n!.StartsWith("TaskFlow.", StringComparison.Ordinal)
                || n == "MediatR")
            .ToList();

        Assert.AreEqual(0, forbiddenReferences.Count, string.Join(Environment.NewLine, forbiddenReferences));

        var forbiddenTypes = EfCqrsAssembly.GetTypes()
            .Where(t => t.Name.Contains("Dispatcher", StringComparison.OrdinalIgnoreCase)
                || t.Name.Contains("RequestBus", StringComparison.OrdinalIgnoreCase)
                || t.GetMethods().Any(m => m.Name == "Send"))
            .Select(t => t.FullName)
            .ToList();

        Assert.AreEqual(0, forbiddenTypes.Count, string.Join(Environment.NewLine, forbiddenTypes));
    }

    /// <summary>Verifies CQRS application does not depend on host or infrastructure behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void CqrsApplication_DoesNotDependOn_HostOrInfrastructure()
    {
        var forbidden = ApplicationCqrsAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .Where(n =>
                n!.StartsWith("TaskFlow.Infrastructure", StringComparison.Ordinal)
                || n.StartsWith("TaskFlow.Api", StringComparison.Ordinal)
                || n.StartsWith("TaskFlow.Gateway", StringComparison.Ordinal)
                || n.StartsWith("TaskFlow.Scheduler", StringComparison.Ordinal)
                || n.StartsWith("TaskFlow.Functions", StringComparison.Ordinal)
                || n.StartsWith("TaskFlow.Bootstrapper", StringComparison.Ordinal))
            .ToList();

        Assert.AreEqual(0, forbidden.Count, string.Join(Environment.NewLine, forbidden));
    }

    /// <summary>Verifies CQRS application uses no dispatcher or mediat r behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void CqrsApplication_UsesNoDispatcherOrMediatR()
    {
        var referenced = ApplicationCqrsAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();
        CollectionAssert.DoesNotContain(referenced, "MediatR");

        var offenders = ApplicationCqrsAssembly.GetTypes()
            .Where(t => t.Name.Contains("Dispatcher", StringComparison.OrdinalIgnoreCase)
                || t.Name.Contains("RequestBus", StringComparison.OrdinalIgnoreCase)
                || t.GetMethods().Any(m => m.Name == "Send"))
            .Select(t => t.FullName)
            .ToList();

        Assert.AreEqual(0, offenders.Count, string.Join(Environment.NewLine, offenders));
    }

    /// <summary>Verifies CQRS handlers implement exactly one request handler contract behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void CqrsHandlers_ImplementExactlyOneRequestHandlerContract()
    {
        var requestHandlerDefinition = typeof(IRequestHandler<,>);
        var handlers = ApplicationCqrsAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToList();

        foreach (var handler in handlers)
        {
            var contracts = handler.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == requestHandlerDefinition)
                .ToList();

            Assert.AreEqual(1, contracts.Count, $"{handler.FullName} must implement one IRequestHandler<,>.");
        }
    }

    /// <summary>Verifies CQRS types do not implement application service contracts behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void CqrsTypes_DoNotImplementApplicationServiceContracts()
    {
        var offenders = ApplicationCqrsAssembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.Namespace == "TaskFlow.Application.Contracts.Services"
                && i.Name.EndsWith("Service", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();

        Assert.AreEqual(0, offenders.Count, string.Join(Environment.NewLine, offenders));
    }

    /// <summary>Verifies CQRS handler catalog has one registration per request behavior and protects the expected test contract.</summary>
    [TestMethod]
    public void CqrsHandlerCatalog_HasOneRegistrationPerRequest()
    {
        var duplicates = CqrsHandlerRegistrationCatalog.Registrations
            .GroupBy(r => r.RequestType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.FullName)
            .ToList();

        Assert.AreEqual(0, duplicates.Count, string.Join(Environment.NewLine, duplicates));
    }
}
