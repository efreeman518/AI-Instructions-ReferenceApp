using BenchmarkDotNet.Running;

namespace Test.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(GetBenchmarkProjectDirectory());
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    private static string GetBenchmarkProjectDirectory() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
}
