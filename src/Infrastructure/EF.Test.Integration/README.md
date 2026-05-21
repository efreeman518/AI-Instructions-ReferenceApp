# EF.Test.Integration

Reusable integration-test support extracted from the TaskFlow reference app.

## What Belongs Here

- `EfWebApplicationFactoryBase<TProgram,TTrxnContext,TQueryContext>` for replacing production pooled EF infrastructure in endpoint/E2E tests.
- `EfTestDbContextFactory<TContext>` and descriptor-removal helpers.
- SQL Server Testcontainers fixture and EF Core options helpers.
- SQL Server options enable EF Core retry-on-failure by default to absorb container warm-up races.
- Aspire.Hosting.Testing helpers for resource health and required connection strings.
- Small environment/path utilities needed by integration test hosts.

## Expected Usage

```csharp
public sealed class SqlApiFactory
    : EfWebApplicationFactoryBase<Program, AppDbContextTrxn, AppDbContextQuery>
{
    protected override string? StartupTaskServiceTypeFullName => "App.Bootstrapper.IStartupTask";

    protected override DbContextOptions BuildTrxnOptions() =>
        DbContextOptionsFactory.BuildSqlServerOptions<AppDbContextTrxn>(Sql.ConnectionString);

    protected override DbContextOptions BuildQueryOptions() =>
        DbContextOptionsFactory.BuildSqlServerOptions<AppDbContextQuery>(Sql.ConnectionString);
}
```

## Non-Goals

- No test framework dependency.
- No application-specific AppHost references.
- No domain-specific builders or seed data.
