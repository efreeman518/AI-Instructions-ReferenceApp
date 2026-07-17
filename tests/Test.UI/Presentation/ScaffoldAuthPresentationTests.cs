namespace Test.UI.Presentation;

[TestClass]
[TestCategory("UI")]
public sealed class ScaffoldAuthPresentationTests
{
    [TestMethod]
    [TestCategory("Presentation")]
    public void MainPage_HasNoLoginAffordance()
    {
        var markup = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Presentation",
            "MainPage.xaml"));

        Assert.IsFalse(markup.Contains("Sign In", StringComparison.Ordinal));
        Assert.IsFalse(markup.Contains("Sign in", StringComparison.Ordinal));
        Assert.IsFalse(markup.Contains("Login", StringComparison.Ordinal));
    }
}
