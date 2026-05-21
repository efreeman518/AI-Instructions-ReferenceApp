using Microsoft.EntityFrameworkCore;

namespace EF.Test.Integration.AspNetCore;

public sealed class EfTestDbContextFactory<TContext>(DbContextOptions options) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext() => WebApplicationFactoryHelpers.CreateContext<TContext>(options);
}
