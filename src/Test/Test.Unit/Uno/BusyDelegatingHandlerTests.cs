using System.Net;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Client.Http;

namespace Test.Unit.Uno;

/// <summary>
/// Validates <c>BusyDelegatingHandler</c> increments <c>BusyTracker.Pending</c> while a request is
/// in-flight, decrements after success, and decrements after the inner handler throws.
/// Pure-unit tier: a stub <see cref="System.Net.Http.HttpMessageHandler"/> short-circuits the pipeline —
/// no real socket, no real server.
/// </summary>
[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class BusyDelegatingHandlerTests
{
    [TestMethod]
    public async Task SendAsync_Increments_During_Request_Decrements_After()
    {
        var tracker = new BusyTracker();
        var gate = new TaskCompletionSource<int>();

        var stub = new StubHandler(async _ =>
        {
            // Release the caller once the handler has entered — proves Pending
            // reaches 1 during the request.
            gate.SetResult(tracker.Pending);
            await Task.Yield();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new BusyDelegatingHandler(tracker) { InnerHandler = stub };
        var invoker = new HttpMessageInvoker(handler);

        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://x/y"), default);

        Assert.AreEqual(1, await gate.Task);
        Assert.AreEqual(0, tracker.Pending);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task SendAsync_Decrements_On_Inner_Exception()
    {
        var tracker = new BusyTracker();
        var stub = new StubHandler(_ => throw new HttpRequestException("network fail"));
        var handler = new BusyDelegatingHandler(tracker) { InnerHandler = stub };
        var invoker = new HttpMessageInvoker(handler);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://x/y"), default));

        Assert.AreEqual(0, tracker.Pending);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            respond(request);
    }
}
