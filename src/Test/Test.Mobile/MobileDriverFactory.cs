using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;

namespace Test.Mobile;

internal static class MobileDriverFactory
{
    public static AppiumDriver Create(MobileTestSettings settings)
    {
        return settings.Platform switch
        {
            MobileTestPlatform.Android => CreateAndroid(settings),
            MobileTestPlatform.Ios => CreateIos(settings),
            _ => throw new InvalidOperationException($"Unsupported mobile platform '{settings.Platform}'.")
        };
    }

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

        return new AndroidDriver(settings.AppiumServerUri, options, settings.StartupTimeout);
    }

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
