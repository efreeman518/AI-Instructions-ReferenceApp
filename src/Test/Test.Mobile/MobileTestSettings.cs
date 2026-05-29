namespace Test.Mobile;

/// <summary>Supports test execution for Test.mobile scenarios.</summary>
internal sealed record MobileTestSettings
{
    public required bool Enabled { get; init; }
    public required MobileTestPlatform Platform { get; init; }
    public required Uri AppiumServerUri { get; init; }
    public required string AppPath { get; init; }
    public required string DeviceName { get; init; }
    public required string ScreenshotDirectory { get; init; }
    public string? PlatformVersion { get; init; }
    public string AndroidAppPackage { get; init; } = "com.taskflow.uno";
    public string AndroidAppWaitActivity { get; init; } = "*";
    public string IosBundleId { get; init; } = "com.taskflow.uno";
    public TimeSpan StartupTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>Verifies from behavior and protects the expected test contract.</summary>
    public static MobileTestSettings From(TestContext context)
    {
        var platform = ParsePlatform(GetValue(context, "TASKFLOW_MOBILE_PLATFORM") ?? "Android");
        var sourceRoot = FindSourceRoot();
        var appPath = GetValue(context, PlatformAppPathKey(platform)) ?? GetDefaultAppPath(sourceRoot, platform);
        var screenshotDirectory = GetValue(context, "TASKFLOW_MOBILE_SCREENSHOT_DIR")
            ?? Path.Combine(sourceRoot, "Test", "Test.Mobile", "TestResults", "screenshots");

        return new MobileTestSettings
        {
            Enabled = IsTrue(GetValue(context, "TASKFLOW_MOBILE_TESTS_ENABLED")),
            Platform = platform,
            AppiumServerUri = new Uri(GetValue(context, "TASKFLOW_APPIUM_SERVER_URL") ?? "http://127.0.0.1:4723/"),
            AppPath = appPath,
            DeviceName = GetValue(context, "TASKFLOW_MOBILE_DEVICE_NAME") ?? DefaultDeviceName(platform),
            PlatformVersion = GetValue(context, "TASKFLOW_MOBILE_PLATFORM_VERSION"),
            ScreenshotDirectory = screenshotDirectory,
            AndroidAppPackage = GetValue(context, "TASKFLOW_ANDROID_APP_PACKAGE") ?? "com.taskflow.uno",
            AndroidAppWaitActivity = GetValue(context, "TASKFLOW_ANDROID_APP_WAIT_ACTIVITY") ?? "*",
            IosBundleId = GetValue(context, "TASKFLOW_IOS_BUNDLE_ID") ?? "com.taskflow.uno",
            StartupTimeout = TimeSpan.FromSeconds(ParsePositiveInt(GetValue(context, "TASKFLOW_MOBILE_STARTUP_TIMEOUT_SECONDS"), 60))
        };
    }

    public string DisabledMessage =>
        "Mobile UI tests are opt-in. Set TASKFLOW_MOBILE_TESTS_ENABLED=true, start Appium, build the app package, " +
        "then run dotnet test src/Test/Test.Mobile/Test.Mobile.csproj --filter TestCategory=MobileUI. " +
        "For Android, restore first with -p:BuildAllUnoTargets=true before the TargetFrameworkOverride build.";

    /// <summary>Verifies get value behavior and protects the expected test contract.</summary>
    private static string? GetValue(TestContext context, string key)
    {
        var env = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env;
        }

        if (context.Properties.TryGetValue(key, out var value) && value is not null)
        {
            var text = value.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    /// <summary>Verifies parse platform behavior and protects the expected test contract.</summary>
    private static MobileTestPlatform ParsePlatform(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "android" => MobileTestPlatform.Android,
            "ios" => MobileTestPlatform.Ios,
            _ => throw new InvalidOperationException($"Unsupported TASKFLOW_MOBILE_PLATFORM '{value}'. Use Android or iOS.")
        };

    /// <summary>Verifies is true behavior and protects the expected test contract.</summary>
    private static bool IsTrue(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    /// <summary>Verifies parse positive int behavior and protects the expected test contract.</summary>
    private static int ParsePositiveInt(string? value, int fallback) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;

    /// <summary>Verifies platform app path key behavior and protects the expected test contract.</summary>
    private static string PlatformAppPathKey(MobileTestPlatform platform) =>
        platform == MobileTestPlatform.Android ? "TASKFLOW_ANDROID_APP_PATH" : "TASKFLOW_IOS_APP_PATH";

    /// <summary>Verifies default device name behavior and protects the expected test contract.</summary>
    private static string DefaultDeviceName(MobileTestPlatform platform) =>
        platform == MobileTestPlatform.Android ? "Android Emulator" : "iPhone Simulator";

    /// <summary>Verifies get default app path behavior and protects the expected test contract.</summary>
    private static string GetDefaultAppPath(string sourceRoot, MobileTestPlatform platform) =>
        platform switch
        {
            MobileTestPlatform.Android => Path.Combine(
                sourceRoot,
                "UI",
                "TaskFlow.Uno",
                "bin",
                "Debug",
                "net10.0-android",
                "com.taskflow.uno-Signed.apk"),
            MobileTestPlatform.Ios => Path.Combine(
                sourceRoot,
                "UI",
                "TaskFlow.Uno",
                "bin",
                "Debug",
                "net10.0-ios",
                "iossimulator-x64",
                "TaskFlow.Uno.app"),
            _ => throw new InvalidOperationException($"Unsupported platform '{platform}'.")
        };

    /// <summary>Verifies find source root behavior and protects the expected test contract.</summary>
    private static string FindSourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TaskFlow.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate TaskFlow.slnx from the test output directory.");
    }
}
