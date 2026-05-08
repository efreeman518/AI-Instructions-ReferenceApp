using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Data;

namespace Test.Integration;

/// <summary>
/// Internal helper that builds <c>TaskFlowDbContextTrxn</c>/<c>TaskFlowDbContextQuery</c> instances
/// pointed at the Aspire-managed SQL container's connection string (<c>AspireTestHost.ConnectionString</c>),
/// so SQL-only and projection tests can use real EF semantics without spinning their own Testcontainers
/// instance.
/// </summary>
internal static class DbContextFactory
{
    internal static TaskFlowDbContextTrxn CreateTrxnContext(string? connString = null)
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContextTrxn>()
            .UseSqlServer(connString ?? AspireTestHost.ConnectionString)
            .Options;
        return new TaskFlowDbContextTrxn(options) { AuditId = "integration-test" };
    }

    internal static TaskFlowDbContextQuery CreateQueryContext(string? connString = null)
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContextQuery>()
            .UseSqlServer(connString ?? AspireTestHost.ConnectionString)
            .Options;
        return new TaskFlowDbContextQuery(options) { AuditId = "integration-test" };
    }
}
