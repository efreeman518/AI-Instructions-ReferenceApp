namespace EF.CQRS.Validation;

public interface IValidationFailureResponseFactory<out TResponse>
{
    TResponse CreateFailure(IReadOnlyCollection<string> errors);
}
