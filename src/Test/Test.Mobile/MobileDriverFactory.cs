using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;

namespace Test.Mobile;

/// <summary>Builds mobile driver test hosts with deterministic dependencies for repeatable test execution.</summary>
internal static class MobileDriverFactory
{
    /// <summary>Creates test dependencies used by the surrounding test cases.</summary>
    public static AppiumDriver Create(MobileTestSettings settings)
    {
        return settings.Platform switch
        {
            MobileTestPlatform.Android => CreateAndroid(settings),
            MobileTestPlatform.Ios => CreateIos(settings),
            _ => throw new InvalidOperationException($"Unsupported mobile platform '{settings.Platform}'.")
        };
    }

    /// <summary>Creates android used by the surrounding test cases.</summary>
    private static AndroidDriver CreateAndroid(MobileTestSettings settings)
    {
        var options = new AppiumOptions
        {
            PlatformName = "Android",
            AutomationName = "UiAutomator2",
            DeviceName = settings.DeviceName,
            App = settings.AppPath
        };

        options.PlatformVersion = settings.PlatformVersion;
        options.AddAdditionalAppiumOption("appPackage", settings.AndroidAppPackage);
        options.AddAdditionalAppiumOption("appWaitActivity", settings.AndroidAppWaitActivity);
        options.AddAdditionalAppiumOption("autoGrantPermissions", true);
        options.AddAdditionalAppiumOption("newCommandTimeout", 120);
        options.AddAdditionalAppiumOption("adbExecTimeout", (int)settings.AdbExecTimeout.TotalMilliseconds);
        options.AddAdditionalAppiumOption("uiautomator2ServerLaunchTimeout", (int)settings.UiAutomator2ServerLaunchTimeout.TotalMilliseconds);
        options.AddAdditionalAppiumOption("androidInstallTimeout", (int)settings.AndroidInstallTimeout.TotalMilliseconds);

        return new AndroidDriver(settings.AppiumServerUri, options, settings.StartupTimeout);
    }

    /// <summary>Creates ios used by the surrounding test cases.</summary>
    private static IOSDriver CreateIos(MobileTestSettings settings)
    {
        var options = new AppiumOptions
        {
            PlatformName = "iOS",
            AutomationName = "XCUITest",
            DeviceName = settings.DeviceName,
            App = settings.AppPath
        };

        options.PlatformVersion = settings.PlatformVersion;
        options.AddAdditionalAppiumOption("bundleId", settings.IosBundleId);
        options.AddAdditionalAppiumOption("newCommandTimeout", 120);

        return new IOSDriver(settings.AppiumServerUri, options, settings.StartupTimeout);
    }
}
