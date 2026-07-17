namespace TaskFlow.Application.Contracts;

/// <summary>Identifies the authentication behavior exposed by TaskFlow hosts and clients.</summary>
public enum AuthMode
{
    Scaffold
}

/// <summary>Resolves the supported authentication mode and rejects configuration drift.</summary>
public static class AuthModeResolver
{
    public const string ConfigKey = "AuthMode";
    public const AuthMode DefaultMode = AuthMode.Scaffold;

    /// <summary>Returns scaffold mode when unset and rejects every unsupported configured value.</summary>
    public static AuthMode Resolve(string? configuredMode)
    {
        if (string.IsNullOrWhiteSpace(configuredMode))
        {
            return DefaultMode;
        }

        if (string.Equals(configuredMode, nameof(AuthMode.Scaffold), StringComparison.OrdinalIgnoreCase))
        {
            return AuthMode.Scaffold;
        }

        throw new InvalidOperationException(
            $"Unsupported AuthMode '{configuredMode}'. Use '{AuthMode.Scaffold}'.");
    }
}
