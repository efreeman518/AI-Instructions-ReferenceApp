using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Core.Client.Http;

public sealed class BusyDelegatingHandler(IBusyTracker busy) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var _ = busy.Begin();
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
