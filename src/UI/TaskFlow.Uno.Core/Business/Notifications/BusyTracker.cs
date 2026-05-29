using System.ComponentModel;

namespace TaskFlow.Uno.Core.Business.Notifications;

/// <summary>Represents or dispatches busy tracker state for the Uno client.</summary>
public sealed class BusyTracker : IBusyTracker
{
    private readonly IUiDispatcher _dispatcher;
    private int _pending;

    public int Pending => Volatile.Read(ref _pending);
    public bool IsActive => Pending > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Initializes busy tracker with required dependencies and default state.</summary>
    public BusyTracker() : this(IUiDispatcher.Inline) { }

    /// <summary>Initializes busy tracker with required dependencies and default state.</summary>
    public BusyTracker(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>Marks the beginning of begin and raises state changes.</summary>
    public IDisposable Begin()
    {
        var before = Interlocked.Increment(ref _pending);
        Raise();
        if (before == 1) RaiseIsActive();
        return new Scope(this);
    }

    /// <summary>Marks the end of end and raises state changes.</summary>
    private void End()
    {
        var after = Interlocked.Decrement(ref _pending);
        Raise();
        if (after == 0) RaiseIsActive();
    }

    /// <summary>Raises raise events for subscribers.</summary>
    private void Raise() => OnUi(() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pending))));

    /// <summary>Raises raise is active events for subscribers.</summary>
    private void RaiseIsActive() => OnUi(() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive))));

    /// <summary>Handles UI events for busy tracker.</summary>
    private void OnUi(Action action)
    {
        if (_dispatcher.HasThreadAccess) action();
        else _dispatcher.Post(action);
    }

    /// <summary>Represents or dispatches scope state for the Uno client.</summary>
    private sealed class Scope(BusyTracker owner) : IDisposable
    {
        private int _disposed;

        /// <summary>Releases scope resources and updates owning state.</summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.End();
        }
    }
}
