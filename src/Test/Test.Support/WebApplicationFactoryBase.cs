using EF.Data;
using EF.Test.Integration.AspNetCore;
using Microsoft.EntityFrameworkCore;

namespace Test.Support;

/// <summary>
/// TaskFlow-specific adapter over the reusable EF.Test.Integration WebApplicationFactory base.
/// Keeps test factories stable while moving shared EF host-replacement plumbing into the package project.
/// </summary>
public abstract class WebApplicationFactoryBase<TProgram, TTrxnContext, TQueryContext>
    : EfWebApplicationFactoryBase<TProgram, TTrxnContext, TQueryContext>
    where TProgram : class
    where TTrxnContext : DbContextBase<string, Guid?>
    where TQueryContext : DbContextBase<string, Guid?>
{
    protected override string? StartupTaskServiceTypeFullName => "TaskFlow.Bootstrapper.IStartupTask";
}
