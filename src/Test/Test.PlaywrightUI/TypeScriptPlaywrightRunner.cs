using System.ComponentModel;
using System.Diagnostics;

namespace Test.PlaywrightUI;

/// <summary>
/// Runs the existing TypeScript Playwright suite from MSTest after Aspire has selected runnable hosts.
/// </summary>
internal static class TypeScriptPlaywrightRunner
{
    internal static string ProjectDirectory => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", ".."));

    internal static bool IsInstalled =>
        File.Exists(PlaywrightCliPath);

    private static string PlaywrightCliPath =>
        Path.Combine(ProjectDirectory, "node_modules", "@playwright", "test", "cli.js");

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
}

internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
