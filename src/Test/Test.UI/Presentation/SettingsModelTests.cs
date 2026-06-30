using Microsoft.Extensions.Configuration;
using Moq;
using TaskFlow.Uno.Presentation.Presentation;
using Uno.Extensions.Navigation;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;

namespace Test.UI.Presentation;

[TestClass]
[TestCategory("UI")]
public sealed class SettingsModelTests
{
    [TestMethod]
    [TestCategory("Presentation")]
    public async Task Given_GatewayConfiguration_When_GatewayUrlRead_Then_ValueLoaded()
    {
        var configuration = new ConfigurationManager
        {
            ["GatewayBaseUrl"] = "https://gateway.test",
        };

        var model = new SettingsModel(Mock.Of<INavigator>(), configuration);

        using var context = SourceContext.GetOrCreate(model).AsCurrent();

        var gatewayUrl = await model.GatewayUrl;

        Assert.AreEqual("https://gateway.test", gatewayUrl);
    }
}
