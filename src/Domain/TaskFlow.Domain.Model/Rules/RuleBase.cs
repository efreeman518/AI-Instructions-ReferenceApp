using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model.Rules;

/// <summary>Models rule base domain behavior and invariants.</summary>
public abstract class RuleBase<T> : IRule<T>
{
    /// <summary>Evaluates the supplied value against rule base and returns pass or failure state.</summary>
    public abstract DomainResult Evaluate(T entity);

    /// <summary>Creates a successful rule result for completed validation.</summary>
    protected static DomainResult Pass() => DomainResult.Success();
    /// <summary>Creates a failed rule result with the validation message.</summary>
    protected static DomainResult Fail(string message) => DomainResult.Failure(message);
}
