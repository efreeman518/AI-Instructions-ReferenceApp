using System.ComponentModel;

namespace TaskFlow.Uno.Core.Business.Notifications;

/// <summary>Defines the busy tracker contract used by TaskFlow components.</summary>
public interface IBusyTracker : INotifyPropertyChanged
{
    int Pending { get; }
    bool IsActive { get; }
    /// <summary>Marks the beginning of begin and raises state changes.</summary>
    IDisposable Begin();
}
