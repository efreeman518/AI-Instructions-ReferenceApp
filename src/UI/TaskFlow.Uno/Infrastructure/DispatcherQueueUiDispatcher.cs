using Microsoft.UI.Dispatching;
using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Infrastructure;

/// <summary>Adapts dispatcher queue UI infrastructure behavior for the Uno client.</summary>
internal sealed class DispatcherQueueUiDispatcher(DispatcherQueue queue) : IUiDispatcher
{
    public bool HasThreadAccess => queue.HasThreadAccess;
    /// <summary>Sends a POST request through dispatcher queue UI dispatcher and returns the typed response.</summary>
    public void Post(Action action) => queue.TryEnqueue(() => action());
}
