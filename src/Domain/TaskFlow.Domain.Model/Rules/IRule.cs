using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model.Rules;

/// <summary>Defines the rule contract used by TaskFlow components.</summary>
public interface IRule<T>
{
    /// <summary>Evaluates the supplied value against rule and returns pass or failure state.</summary>
    DomainResult Evaluate(T entity);
}
