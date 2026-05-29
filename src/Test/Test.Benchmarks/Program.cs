using BenchmarkDotNet.Running;

namespace Test.Benchmarks;

/// <summary>Supports test execution for Test.benchmarks scenarios.</summary>
public class Program
{
    /// <summary>Supports benchmark execution for program.</summary>
    public static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(GetBenchmarkProjectDirectory());
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    /// <summary>Supports benchmark execution for program.</summary>
    private static string GetBenchmarkProjectDirectory() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
}
