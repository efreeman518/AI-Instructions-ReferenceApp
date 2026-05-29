namespace TaskFlow.Uno.Core.Business.Notifications;

/// <summary>Represents or dispatches problem details state for the Uno client.</summary>
public sealed class ProblemDetailsException : Exception
{
    public ProblemDetailsPayload Problem { get; }
    public int StatusCode { get; }

    /// <summary>Initializes problem details exception with required dependencies and default state.</summary>
    public ProblemDetailsException(ProblemDetailsPayload problem, int statusCode)
        : base(problem.Detail ?? problem.Title ?? $"HTTP {statusCode}")
    {
        Problem = problem;
        StatusCode = statusCode;
    }
}
