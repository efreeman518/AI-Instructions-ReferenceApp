using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace TaskFlow.Infrastructure.Data;

/// <summary>Moves the legacy primary migration history into its schema-pinned location.</summary>
public static class TaskFlowMigrationHistoryCompatibility
{
    /// <summary>
    /// Relocates an existing dbo history table before EF reads the schema-pinned history. New
    /// databases and databases already using the pinned location are unchanged.
    /// </summary>
    public static async Task RelocateLegacyHistoryTableAsync(
        IDbContextFactory<TaskFlowDbContextTrxn> contextFactory,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var databaseCreator = db.GetService<IRelationalDatabaseCreator>();
        if (!await databaseCreator.ExistsAsync(cancellationToken))
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'[taskflow].[__EFMigrationsHistory]', N'U') IS NOT NULL
        THROW 51000, 'Both legacy and schema-pinned TaskFlow migration history tables exist.', 1;

    IF SCHEMA_ID(N'taskflow') IS NULL
        EXEC(N'CREATE SCHEMA [taskflow]');

    ALTER SCHEMA [taskflow] TRANSFER [dbo].[__EFMigrationsHistory];
END;
""",
            cancellationToken);
    }
}
