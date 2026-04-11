using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model.Rules;

public abstract class RuleBase<T> : IRule<T>
{
    public abstract DomainResult Evaluate(T entity);

    protected static DomainResult Pass() => DomainResult.Success();
    protected static DomainResult Fail(string message) => DomainResult.Failure(message);
}
