namespace TaskFlow.Uno.Core.Business.Notifications;

/// <summary>Defines the UI dispatcher contract used by TaskFlow components.</summary>
public interface IUiDispatcher
{
    bool HasThreadAccess { get; }
    /// <summary>Sends a POST request through UI dispatcher and returns the typed response.</summary>
    void Post(Action action);

    public static IUiDispatcher Inline { get; } = new InlineDispatcher();

    /// <summary>Represents or dispatches inline state for the Uno client.</summary>
    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool HasThreadAccess => true;
        /// <summary>Sends a POST request through inline dispatcher and returns the typed response.</summary>
        public void Post(Action action) => action();
    }
}
