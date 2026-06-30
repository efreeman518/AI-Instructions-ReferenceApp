namespace TaskFlow.Uno.Presentation;

// Cross-model guard consulted by the shell chrome (menu buttons) before
// switching sibling routes. A detail model registers IsDirtyAsync when it
// becomes active so the shell can prompt before discarding in-flight edits.
public interface IFormGuard
{
    Func<CancellationToken, ValueTask<bool>>? IsDirtyAsync { get; set; }

    /// <summary>Provides the clear operation for form guard.</summary>
    void Clear();
}

/// <summary>Drives form guard state, navigation, and commands for the Uno presentation layer.</summary>
internal sealed class FormGuard : IFormGuard
{
    public Func<CancellationToken, ValueTask<bool>>? IsDirtyAsync { get; set; }

    /// <summary>Provides the clear operation for form guard.</summary>
    public void Clear() => IsDirtyAsync = null;
}
