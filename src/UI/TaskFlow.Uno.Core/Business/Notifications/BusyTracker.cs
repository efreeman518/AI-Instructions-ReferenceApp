using System.ComponentModel;

namespace TaskFlow.Uno.Core.Business.Notifications;

public sealed class BusyTracker : IBusyTracker
{
    private readonly IUiDispatcher _dispatcher;
    private int _pending;

    public int Pending => Volatile.Read(ref _pending);
    public bool IsActive => Pending > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public BusyTracker() : this(IUiDispatcher.Inline) { }

    public BusyTracker(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public IDisposable Begin()
    {
        var before = Interlocked.Increment(ref _pending);
        Raise();
        if (before == 1) RaiseIsActive();
        return new Scope(this);
    }

    private void End()
    {
        var after = Interlocked.Decrement(ref _pending);
        Raise();
        if (after == 0) RaiseIsActive();
    }

    private void Raise() => OnUi(() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pending))));

    private void RaiseIsActive() => OnUi(() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive))));

    private void OnUi(Action action)
    {
        if (_dispatcher.HasThreadAccess) action();
        else _dispatcher.Post(action);
    }

    private sealed class Scope(BusyTracker owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.End();
        }
    }
}
