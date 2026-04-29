using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Data;
using Test.Support;

namespace Test.Endpoints;

/// <summary>
/// In-memory WebApplicationFactory for endpoint contract tests.
///
/// Uses EF Core <c>InMemoryDatabase</c> per factory instance so each test class gets an isolated DB.
/// For multi-endpoint workflow tests against a real SQL database, see <c>SqlApiFactory</c> in Test.E2E.
/// </summary>
public sealed class CustomApiFactory : WebApplicationFactoryBase<Program, TaskFlowDbContextTrxn, TaskFlowDbContextQuery>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override DbContextOptions BuildTrxnOptions() =>
        new DbContextOptionsBuilder<TaskFlowDbContextTrxn>().UseInMemoryDatabase(_dbName).Options;

    protected override DbContextOptions BuildQueryOptions() =>
        new DbContextOptionsBuilder<TaskFlowDbContextQuery>().UseInMemoryDatabase(_dbName).Options;
}
