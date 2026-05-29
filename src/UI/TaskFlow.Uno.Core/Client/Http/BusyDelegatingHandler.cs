using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Core.Client.Http;

/// <summary>Handles busy delegating work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
public sealed class BusyDelegatingHandler(IBusyTracker busy) : DelegatingHandler
{
    /// <summary>Processes HTTP requests through busy delegating handler and applies its cross-cutting policy.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var _ = busy.Begin();
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
