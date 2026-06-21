namespace Test.Aspire;

/// <summary>Fast checks for the Aspire test harness AI provider defaults.</summary>
[TestClass]
[TestCategory("Foundry")]
[DoNotParallelize]
public sealed class AspireAiProviderSelectionTests
{
    [TestMethod]
    public void Given_NoAzureConfig_When_SelectingLiveAiProvider_Then_FoundryLocalIsDefault()
    {
        using var _ = new EnvironmentOverride(
            ("TASKFLOW_ENABLE_FOUNDRY_LOCAL", null),
            ("TASKFLOW_USE_AZURE_FOUNDRY", null),
            ("AiServices__FoundryEndpoint", null),
            ("AiServices:FoundryEndpoint", null));

        Assert.AreEqual(AspireAiProvider.FoundryLocal, AspireTestHost.SelectRequestedAiProviderForTesting());
        Assert.IsTrue(AspireTestHost.ShouldEnableFoundryLocalForTesting());
    }

    [TestMethod]
    [TestCategory("AzureFoundry")]
    public void Given_AzureConfig_When_SelectingLiveAiProvider_Then_AzureFoundryWins()
    {
        using var _ = new EnvironmentOverride(
            ("TASKFLOW_ENABLE_FOUNDRY_LOCAL", null),
            ("TASKFLOW_USE_AZURE_FOUNDRY", "true"),
            ("AiServices__FoundryEndpoint", null),
            ("AiServices:FoundryEndpoint", null));

        Assert.AreEqual(AspireAiProvider.AzureFoundry, AspireTestHost.SelectRequestedAiProviderForTesting());
        Assert.IsFalse(AspireTestHost.ShouldEnableFoundryLocalForTesting());
    }

    [TestMethod]
    public void Given_ExplicitLocal_When_AzureConfigExists_Then_FoundryLocalWins()
    {
        using var _ = new EnvironmentOverride(
            ("TASKFLOW_ENABLE_FOUNDRY_LOCAL", "true"),
            ("TASKFLOW_USE_AZURE_FOUNDRY", "true"),
            ("AiServices__FoundryEndpoint", null),
            ("AiServices:FoundryEndpoint", null));

        Assert.AreEqual(AspireAiProvider.FoundryLocal, AspireTestHost.SelectRequestedAiProviderForTesting());
        Assert.IsTrue(AspireTestHost.ShouldEnableFoundryLocalForTesting());
    }

    private sealed class EnvironmentOverride : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues = new(StringComparer.Ordinal);

        public EnvironmentOverride(params (string Name, string? Value)[] values)
        {
            foreach (var (name, value) in values)
            {
                _originalValues[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach (var (name, value) in _originalValues)
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}
