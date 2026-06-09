using EF.Data;
using EF.Domain;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>
/// Open-generic transactional repository bound to the TaskFlow write context. Registered as an open
/// generic (<c>typeof(IRepositoryTrxn&lt;&gt;) -&gt; typeof(TaskFlowRepositoryTrxn&lt;&gt;)</c>) so any
/// entity with no bespoke persistence logic resolves <c>IRepositoryTrxn&lt;TEntity&gt;</c> without a
/// per-entity repository class. Closes the two-parameter <see cref="RepositoryTrxn{TEntity, TDbContext}"/>
/// over <see cref="TaskFlowDbContextTrxn"/> so DI sees a one-arity open generic.
/// </summary>
public sealed class TaskFlowRepositoryTrxn<TEntity>(TaskFlowDbContextTrxn db)
    : RepositoryTrxn<TEntity, TaskFlowDbContextTrxn>(db)
    where TEntity : EntityBase
{
}

/// <summary>
/// Open-generic read repository bound to the TaskFlow no-tracking query context. Registered as an open
/// generic for entities with no bespoke read logic. Bespoke <c>I{Entity}RepositoryQuery</c> contracts
/// that need paged search extend <c>IRepositoryQuery&lt;TEntity&gt;</c> and are registered explicitly.
/// </summary>
public sealed class TaskFlowRepositoryQuery<TEntity>(TaskFlowDbContextQuery db)
    : RepositoryQuery<TEntity, TaskFlowDbContextQuery>(db)
    where TEntity : EntityBase
{
}
