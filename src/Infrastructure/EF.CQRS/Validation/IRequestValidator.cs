namespace EF.CQRS.Validation;

public interface IRequestValidator<in TRequest>
{
    Task<RequestValidationResult> ValidateAsync(TRequest request, CancellationToken ct = default);
}
