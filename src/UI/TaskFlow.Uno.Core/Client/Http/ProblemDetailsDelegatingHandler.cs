using System.Net.Http.Json;
using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Core.Client.Http;

public sealed class ProblemDetailsDelegatingHandler(INotificationService notifications) : DelegatingHandler
{
    private const string ProblemContentType = "application/problem+json";
    private const int ClientCancelledStatus = 499;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode) return response;

        // Only intercept RFC 7807 payloads — leave other error shapes untouched.
        if (response.Content?.Headers.ContentType?.MediaType != ProblemContentType)
            return response;

        ProblemDetailsPayload? problem;
        try
        {
            problem = await response.Content
                .ReadFromJsonAsync<ProblemDetailsPayload>(cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // Malformed problem+json — fall back to the raw response, caller's
            // EnsureSuccessStatusCode will surface a plain HttpRequestException.
            return response;
        }

        if (problem is null) return response;

        var status = problem.Status ?? (int)response.StatusCode;

        // Client-initiated cancellation: still signal failure to the caller,
        // but don't annoy the user with a toast for something they triggered.
        if (status != ClientCancelledStatus)
        {
            await notifications.ShowProblem(problem, cancellationToken).ConfigureAwait(false);
        }

        response.Dispose();
        throw new ProblemDetailsException(problem, status);
    }
}
