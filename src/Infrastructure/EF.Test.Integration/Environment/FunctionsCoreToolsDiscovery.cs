namespace EF.Test.Integration.Environment;

public static class FunctionsCoreToolsDiscovery
{
    public static bool EnsureFuncToolAvailable()
    {
        var candidateNames = OperatingSystem.IsWindows()
            ? new[] { "func.exe", "func.cmd", "func.bat" }
            : ["func"];

        var path = System.Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathEntries = path.Split(
            Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var directory in pathEntries)
        {
            foreach (var candidate in candidateNames)
            {
                if (File.Exists(Path.Combine(directory, candidate)))
                    return true;
            }
        }

        if (!OperatingSystem.IsWindows())
            return false;

        var localFunctionsToolsRoot = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "AzureFunctionsTools",
            "Releases");

        if (!Directory.Exists(localFunctionsToolsRoot))
            return false;

        var discoveredDirectory = Directory
            .EnumerateFiles(localFunctionsToolsRoot, "func.exe", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => directory!)
            .OrderByDescending(directory => directory.Contains("4.", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(directory => directory, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (discoveredDirectory is null)
            return false;

        System.Environment.SetEnvironmentVariable(
            "PATH",
            string.IsNullOrWhiteSpace(path)
                ? discoveredDirectory
                : string.Join(Path.PathSeparator, [discoveredDirectory, .. pathEntries]));

        return true;
    }
}
