namespace TaskFlow.Uno.Core.Business.Notifications;

public sealed class ProblemDetailsException : Exception
{
    public ProblemDetailsPayload Problem { get; }
    public int StatusCode { get; }

    public ProblemDetailsException(ProblemDetailsPayload problem, int statusCode)
        : base(problem.Detail ?? problem.Title ?? $"HTTP {statusCode}")
    {
        Problem = problem;
        StatusCode = statusCode;
    }
}
