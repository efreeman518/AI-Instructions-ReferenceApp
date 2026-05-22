using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Test.Mobile;

[TestClass]
[TestCategory("MobileUI")]
public sealed class MobileSmokeTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void TaskFlowMobile_AppLaunches_AndRendersNativeSurface()
    {
        var settings = MobileTestSettings.From(TestContext);
        if (!settings.Enabled)
        {
            TestContext.WriteLine(settings.DisabledMessage);
            return;
        }

        if (!File.Exists(settings.AppPath) && !Directory.Exists(settings.AppPath))
        {
            Assert.Fail($"Mobile app package not found: {settings.AppPath}. Build TaskFlow.Uno for {settings.Platform} first.");
        }

        AppiumDriver? driver = null;

        try
        {
            driver = MobileDriverFactory.Create(settings);
            WaitForNativeSurface(driver, settings);

            var screenshotPath = MobileTestArtifacts.SaveScreenshot(driver, settings, TestContext, "taskflow-mobile-launch");
            Assert.IsTrue(new FileInfo(screenshotPath).Length > 1024, "Launch screenshot was unexpectedly empty.");
        }
        catch (WebDriverException ex)
        {
            Assert.Fail(
                "Appium mobile smoke failed. Confirm Appium server, platform driver, emulator/simulator, and app path. " +
                $"Details: {ex.Message}");
        }
        finally
        {
            if (driver is not null)
            {
                TrySavePageSource(driver, settings);
                TryQuit(driver);
            }
        }
    }

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
