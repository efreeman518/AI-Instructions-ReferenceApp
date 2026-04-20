using System.ComponentModel;

namespace TaskFlow.Uno.Core.Business.Notifications;

public interface IBusyTracker : INotifyPropertyChanged
{
    int Pending { get; }
    bool IsActive { get; }
    IDisposable Begin();
}
