namespace TaskFlow.Application.Contracts;

public enum ApplicationStyle
{
    Service,
    Cqrs
}

public static class ApplicationStyleResolver
{
    public const string ConfigKey = "Application:Style";
    public const string EnvironmentVariable = "TASKFLOW_APPLICATION_STYLE";
    public const ApplicationStyle DefaultStyle = ApplicationStyle.Service;

    public static ApplicationStyle Resolve(string? configuredStyle)
    {
        var raw = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = configuredStyle;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultStyle;
        }

        if (Enum.TryParse<ApplicationStyle>(raw, ignoreCase: true, out var style))
        {
            return style;
        }

        throw new InvalidOperationException(
            $"Unsupported application style '{raw}'. Use '{ApplicationStyle.Service}' or '{ApplicationStyle.Cqrs}'.");
    }
}
