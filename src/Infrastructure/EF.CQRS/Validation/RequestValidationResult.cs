namespace EF.CQRS.Validation;

public sealed class RequestValidationResult
{
    private RequestValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; }

    public static RequestValidationResult Valid() => new(Array.Empty<string>());

    public static RequestValidationResult Failure(string error) =>
        Failure([error]);

    public static RequestValidationResult Failure(IEnumerable<string> errors) =>
        new(errors.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray());

    public static RequestValidationResult Combine(IEnumerable<RequestValidationResult> results) =>
        Failure(results.SelectMany(result => result.Errors));
}
