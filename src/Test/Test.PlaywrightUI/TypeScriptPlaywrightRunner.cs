using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace Test.PlaywrightUI;

/// <summary>
/// Runs the existing TypeScript Playwright suite from MSTest after Aspire has selected runnable hosts.
/// </summary>
internal static class TypeScriptPlaywrightRunner
{
    internal static string ProjectDirectory => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", ".."));

    private static string PlaywrightCliPath =>
        Path.Combine(ProjectDirectory, "node_modules", "@playwright", "test", "cli.js");

    internal static BrowserReadiness CheckReadiness()
    {
        if (!File.Exists(PlaywrightCliPath))
        {
            return new BrowserReadiness(false,
                "TypeScript Playwright dependencies are missing. Run: rtk npm install in src\\Test\\Test.PlaywrightUI.");
        }

        var managedBrowser = GetManagedHeadlessShellPath();
        if (managedBrowser is not null && File.Exists(managedBrowser))
        {
            return new BrowserReadiness(true, $"Using Playwright-managed Chromium: {managedBrowser}");
        }

        var systemChrome = GetSystemChromePath();
        if (systemChrome is not null && !string.Equals(
                Environment.GetEnvironmentVariable("PLAYWRIGHT_USE_SYSTEM_CHROME"),
                "false",
                StringComparison.OrdinalIgnoreCase))
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_USE_SYSTEM_CHROME", "true");
            return new BrowserReadiness(true,
                $"Playwright-managed Chromium is missing; using installed Chrome: {systemChrome}");
        }

        return new BrowserReadiness(false,
            "Playwright Chromium is missing and no system Chrome fallback is available. Run: rtk npx playwright install chromium in src\\Test\\Test.PlaywrightUI.");
    }

    internal static async Task<CommandResult> RunAsync(
        IReadOnlyList<string> projects,
        CancellationToken cancellationToken)
    {
        if (projects.Count == 0)
        {
            return new CommandResult(0, "No TypeScript Playwright projects selected.", "");
        }

        var fileName = OperatingSystem.IsWindows() ? "node.exe" : "node";

        try
        {
            var startInfo = new ProcessStartInfo(fileName)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = ProjectDirectory
            };
            startInfo.ArgumentList.Add(PlaywrightCliPath);
            startInfo.ArgumentList.Add("test");
            foreach (var project in projects)
            {
                startInfo.ArgumentList.Add($"--project={project}");
            }

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Playwright.");

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }

                await process.WaitForExitAsync(CancellationToken.None);
                return new CommandResult(124, await stdout, await stderr);
            }

            return new CommandResult(process.ExitCode, await stdout, await stderr);
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException("Node.js is not available on PATH.", ex);
        }
    }

    private static string? GetManagedHeadlessShellPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var browsersJsonPath = Path.Combine(ProjectDirectory, "node_modules", "playwright-core", "browsers.json");
        if (!File.Exists(browsersJsonPath))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(browsersJsonPath));
        var revision = document.RootElement.GetProperty("browsers")
            .EnumerateArray()
            .FirstOrDefault(browser =>
                browser.GetProperty("name").GetString() == "chromium-headless-shell")
            .GetProperty("revision")
            .GetString();

        if (string.IsNullOrWhiteSpace(revision))
        {
            return null;
        }

        var browserRoot = GetBrowserRoot();
        return Path.Combine(
            browserRoot,
            $"chromium_headless_shell-{revision}",
            "chrome-headless-shell-win64",
            "chrome-headless-shell.exe");
    }

    private static string GetBrowserRoot()
    {
        var configured = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
        if (configured == "0")
        {
            return Path.Combine(ProjectDirectory, "node_modules", "playwright-core", ".local-browsers");
        }

        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ms-playwright")
            : configured;
    }

    private static string? GetSystemChromePath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var paths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe")
        };

        return paths.FirstOrDefault(File.Exists);
    }
}

internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);

internal sealed record BrowserReadiness(bool CanRun, string Message);
