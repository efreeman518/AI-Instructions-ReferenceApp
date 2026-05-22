using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Test.Mobile;

internal static class MobileTestArtifacts
{
    public static string SaveScreenshot(AppiumDriver driver, MobileTestSettings settings, TestContext context, string name)
    {
        Directory.CreateDirectory(settings.ScreenshotDirectory);

        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var screenshotPath = Path.Combine(settings.ScreenshotDirectory, $"{safeName}_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.png");

        var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
        screenshot.SaveAsFile(screenshotPath);
        context.AddResultFile(screenshotPath);

        return screenshotPath;
    }

    public static void SavePageSource(AppiumDriver driver, MobileTestSettings settings, TestContext context, string name)
    {
        Directory.CreateDirectory(settings.ScreenshotDirectory);

        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var sourcePath = Path.Combine(settings.ScreenshotDirectory, $"{safeName}_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.xml");

        File.WriteAllText(sourcePath, driver.PageSource);
        context.AddResultFile(sourcePath);
    }
}
