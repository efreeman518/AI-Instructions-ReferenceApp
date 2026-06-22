using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Test.Mobile.PageObjects;

namespace Test.Mobile;

/// <summary>
/// Shared lifecycle for mobile UI flow tests. Honors the same opt-in contract as
/// <see cref="MobileSmokeTests"/>: when TASKFLOW_MOBILE_TESTS_ENABLED is unset, the test reports
/// Inconclusive without touching Appium. Enabled tests create one Appium session per test for
/// isolation and capture a screenshot + page source when a test fails.
/// </summary>
public abstract class MobileUiTestBase
{
    public TestContext TestContext { get; set; } = null!;

    private protected MobileTestSettings Settings { get; private set; } = null!;
    private protected AppiumDriver? Driver { get; private set; }
    private protected TaskFlowMobileApp App { get; private set; } = null!;

    /// <summary>True when mobile tests are opted in and a driver session is live.</summary>
    private protected bool Enabled => Driver is not null;

    /// <summary>
    /// Runs a flow body when mobile is enabled. Driver, timeout, and assertion failures stay red
    /// because the explicit mobile lane should only be skipped before opt-in.
    /// </summary>
    private protected void RunMobileFlow(Action body)
    {
        if (!Enabled)
        {
            Assert.Inconclusive(Settings.DisabledMessage);
        }

        body();
    }

    [TestInitialize]
    public void Setup()
    {
        Settings = MobileTestSettings.From(TestContext);
        if (!Settings.Enabled)
        {
            TestContext.WriteLine(Settings.DisabledMessage);
            Assert.Inconclusive(Settings.DisabledMessage);
        }

        if (!File.Exists(Settings.AppPath) && !Directory.Exists(Settings.AppPath))
        {
            Assert.Fail($"Mobile app package not found: {Settings.AppPath}. Build TaskFlow.Uno for {Settings.Platform} first.");
        }

        Driver = MobileDriverFactory.Create(Settings);
        App = new TaskFlowMobileApp(Driver, Settings.StartupTimeout);
        App.CollapseStatusBar();
    }

    [TestCleanup]
    public void Teardown()
    {
        if (Driver is null)
        {
            return;
        }

        if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
        {
            TrySaveArtifacts();
        }

        try { Driver.Quit(); }
        catch (WebDriverException ex) { TestContext.WriteLine($"Could not quit Appium driver cleanly: {ex.Message}"); }
        finally
        {
            Driver.Dispose();
            Driver = null;
        }
    }

    private void TrySaveArtifacts()
    {
        try
        {
            MobileTestArtifacts.SaveScreenshot(Driver!, Settings, TestContext, $"{TestContext.TestName}-failure");
            MobileTestArtifacts.SavePageSource(Driver!, Settings, TestContext, $"{TestContext.TestName}-page-source");
        }
        catch (WebDriverException ex)
        {
            TestContext.WriteLine($"Could not capture failure artifacts: {ex.Message}");
        }
    }
}
