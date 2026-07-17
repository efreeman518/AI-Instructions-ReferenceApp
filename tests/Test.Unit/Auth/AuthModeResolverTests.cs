using TaskFlow.Application.Contracts;

namespace Test.Unit.Auth;

[TestClass]
[TestCategory("Unit")]
public sealed class AuthModeResolverTests
{
    [TestMethod]
    public void Resolve_WhenUnset_ReturnsScaffold() =>
        Assert.AreEqual(AuthMode.Scaffold, AuthModeResolver.Resolve(null));

    [TestMethod]
    [DataRow("Scaffold")]
    [DataRow("scaffold")]
    public void Resolve_WhenScaffold_ReturnsValidatedMode(string configuredMode) =>
        Assert.AreEqual(AuthMode.Scaffold, AuthModeResolver.Resolve(configuredMode));

    [TestMethod]
    [DataRow("Local")]
    [DataRow("Entra")]
    [DataRow("0")]
    public void Resolve_WhenUnsupported_Throws(string configuredMode)
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => AuthModeResolver.Resolve(configuredMode));

        StringAssert.Contains(exception.Message, configuredMode);
    }
}
