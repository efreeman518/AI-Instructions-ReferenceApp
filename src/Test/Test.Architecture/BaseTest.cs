using System.Reflection;
using TaskFlow.Application.Contracts;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Repositories;

namespace Test.Architecture;

public abstract class BaseTest
{
    protected static readonly Assembly DomainModelAssembly = typeof(Category).Assembly;
    protected static readonly Assembly ApplicationContractsAssembly = typeof(AppConstants).Assembly;
    protected static readonly Assembly ApplicationServicesAssembly = Assembly.Load("TaskFlow.Application.Services");
    protected static readonly Assembly InfrastructureDataAssembly = typeof(TaskFlowDbContextTrxn).Assembly;
    protected static readonly Assembly InfrastructureRepositoriesAssembly = typeof(CategoryRepositoryTrxn).Assembly;
}
