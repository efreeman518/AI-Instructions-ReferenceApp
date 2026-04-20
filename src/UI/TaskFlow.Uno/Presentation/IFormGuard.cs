namespace TaskFlow.Uno.Presentation;

// Cross-model guard consulted by the shell chrome (menu buttons) before
// switching sibling routes. A detail model registers IsDirtyAsync when it
// becomes active so the shell can prompt before discarding in-flight edits.
public interface IFormGuard
{
    Func<CancellationToken, ValueTask<bool>>? IsDirtyAsync { get; set; }

    void Clear();
}

internal sealed class FormGuard : IFormGuard
{
    public Func<CancellationToken, ValueTask<bool>>? IsDirtyAsync { get; set; }

    public void Clear() => IsDirtyAsync = null;
}
