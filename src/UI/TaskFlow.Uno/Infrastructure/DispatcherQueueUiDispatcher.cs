using Microsoft.UI.Dispatching;
using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Infrastructure;

internal sealed class DispatcherQueueUiDispatcher(DispatcherQueue queue) : IUiDispatcher
{
    public bool HasThreadAccess => queue.HasThreadAccess;
    public void Post(Action action) => queue.TryEnqueue(() => action());
}
