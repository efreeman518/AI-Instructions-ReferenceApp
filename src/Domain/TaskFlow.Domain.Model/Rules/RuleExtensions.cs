using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model.Rules;

/// <summary>Models rule extensions domain behavior and invariants.</summary>
public static class RuleExtensions
{
    /// <summary>Provides the evaluate all operation for rule extensions.</summary>
    public static DomainResult EvaluateAll<T>(this IEnumerable<IRule<T>> rules, T entity)
    {
        var errors = new List<DomainError>();
        foreach (var rule in rules)
        {
            var result = rule.Evaluate(entity);
            if (result.IsFailure)
                errors.Add(DomainError.Create(result.ErrorMessage));
        }
        return errors.Count > 0
            ? DomainResult.Failure(errors)
            : DomainResult.Success();
    }
}
