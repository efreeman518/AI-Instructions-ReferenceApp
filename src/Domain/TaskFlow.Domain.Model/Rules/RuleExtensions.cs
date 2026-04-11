using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model.Rules;

public static class RuleExtensions
{
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
