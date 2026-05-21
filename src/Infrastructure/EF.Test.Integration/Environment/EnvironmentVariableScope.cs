namespace EF.Test.Integration.Environment;

public sealed class EnvironmentVariableScope : IDisposable
{
    private readonly Dictionary<string, string?> _originalValues = new(StringComparer.Ordinal);

    public EnvironmentVariableScope Set(string name, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_originalValues.ContainsKey(name))
            _originalValues[name] = System.Environment.GetEnvironmentVariable(name);

        System.Environment.SetEnvironmentVariable(name, value);
        return this;
    }

    public void Dispose()
    {
        foreach (var (name, value) in _originalValues)
            System.Environment.SetEnvironmentVariable(name, value);
    }
}
