using Microsoft.EntityFrameworkCore;

namespace EF.Test.Integration.EntityFramework;

public static class DbContextOptionsFactory
{
    public static DbContextOptions<TContext> BuildSqlServerOptions<TContext>(
        string connectionString,
        Action<DbContextOptionsBuilder<TContext>>? configure = null)
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(
                connectionString,
                sqlServer => sqlServer.EnableRetryOnFailure());

        configure?.Invoke(builder);
        return builder.Options;
    }

    public static DbContextOptions<TContext> BuildInMemoryOptions<TContext>(
        string? databaseName = null,
        Action<DbContextOptionsBuilder<TContext>>? configure = null)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"));
        configure?.Invoke(builder);
        return builder.Options;
    }
}
