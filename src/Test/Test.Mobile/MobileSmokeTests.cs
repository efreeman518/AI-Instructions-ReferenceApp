using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Test.Mobile;

/// <summary>Covers mobile smoke behavior with focused assertions that document expected behavior and regression intent.</summary>
[TestClass]
[TestCategory("MobileUI")]
public sealed class MobileSmokeTests
{
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Verifies task flow mobile app launches and renders native surface behavior and protects the expected test contract.</summary>
    [TestMethod]
    [Timeout(300_000, CooperativeCancellation = true)]
    public void TaskFlowMobile_AppLaunches_AndRendersNativeSurface()
    {
        var settings = MobileTestSettings.From(TestContext);
        if (!settings.Enabled)
        {
            TestContext.WriteLine(MobileTestSettings.DisabledMessage);
            Assert.Inconclusive(MobileTestSettings.DisabledMessage);
        }

        if (!File.Exists(settings.AppPath) && !Directory.Exists(settings.AppPath))
        {
            Assert.Fail($"Mobile app package not found: {settings.AppPath}. Build TaskFlow.Uno for {settings.Platform} first.");
        }

        AppiumDriver? driver = null;
        string? driverUnavailableReason = null;

        try
        {
            driver = MobileDriverFactory.Create(settings);
            WaitForNativeSurface(driver, settings);

            var screenshotPath = MobileTestArtifacts.SaveScreenshot(driver, settings, TestContext, "taskflow-mobile-launch");
            Assert.IsGreaterThan(1024, new FileInfo(screenshotPath).Length, "Launch screenshot was unexpectedly empty.");
        }
        catch (WebDriverException ex)
        {
            driverUnavailableReason =
                "Mobile driver/instrumentation unavailable. Confirm Appium server, platform driver, emulator/simulator, and app path. " +
                $"Details: {ex.Message}";
        }
        finally
        {
            if (driver is not null)
            {
                TrySavePageSource(driver, settings);
                TryQuit(driver);
            }
        }

        if (driverUnavailableReason is not null)
        {
            Assert.Fail(driverUnavailableReason);
        }
    }

    /// <summary>Verifies try save page source behavior and protects the expected test contract.</summary>
    private void TrySavePageSource(AppiumDriver driver, MobileTestSettings settings)
    {
        try
        {
            MobileTestArtifacts.SavePageSource(driver, settings, TestContext, "taskflow-mobile-page-source");
        }
        catch (WebDriverException ex)
        {
            TestContext.WriteLine($"Could not save Appium page source: {ex.Message}");
        }
    }

    /// <summary>Verifies try quit behavior and protects the expected test contract.</summary>
    private void TryQuit(AppiumDriver driver)
    {
        try
        {
            driver.Quit();
        }
        catch (WebDriverException ex)
        {
            TestContext.WriteLine($"Could not quit Appium driver cleanly: {ex.Message}");
        }
        finally
        {
            driver.Dispose();
        }
    }

    /// <summary>Verifies wait for native surface behavior and protects the expected test contract.</summary>
    private static void WaitForNativeSurface(AppiumDriver driver, MobileTestSettings settings)
    {
        var deadline = DateTimeOffset.UtcNow + settings.StartupTimeout;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                if (HasRenderedSurface(driver.PageSource, settings.Platform))
                {
                    return;
                }
            }
            catch (WebDriverException ex)
            {
                lastException = ex;
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException(
            $"Timed out waiting for the TaskFlow {settings.Platform} native surface to render.",
            lastException);
    }

    /// <summary>Verifies has rendered surface behavior and protects the expected test contract.</summary>
    private static bool HasRenderedSurface(string pageSource, MobileTestPlatform platform)
    {
        if (string.IsNullOrWhiteSpace(pageSource))
        {
            return false;
        }

        if (pageSource.Contains("TaskFlow", StringComparison.OrdinalIgnoreCase)
            || pageSource.Contains("Create new task", StringComparison.OrdinalIgnoreCase)
            || pageSource.Contains("UnoSK", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return platform switch
        {
            MobileTestPlatform.Android => pageSource.Contains("com.taskflow.uno", StringComparison.OrdinalIgnoreCase)
                || pageSource.Contains("android.widget.FrameLayout", StringComparison.OrdinalIgnoreCase),
            MobileTestPlatform.Ios => pageSource.Contains("XCUIElementTypeApplication", StringComparison.OrdinalIgnoreCase)
                || pageSource.Contains("com.taskflow.uno", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
