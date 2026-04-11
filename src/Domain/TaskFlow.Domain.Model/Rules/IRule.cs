using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model.Rules;

public interface IRule<T>
{
    DomainResult Evaluate(T entity);
}
