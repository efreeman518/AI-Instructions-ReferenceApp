using System.ComponentModel;

namespace TaskFlow.Uno.Core.Business.Notifications;

public sealed class BusyTracker : IBusyTracker
{
    private int _pending;

    public int Pending => Volatile.Read(ref _pending);
    public bool IsActive => Pending > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    private void Raise() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pending)));

    private void RaiseIsActive() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));

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
