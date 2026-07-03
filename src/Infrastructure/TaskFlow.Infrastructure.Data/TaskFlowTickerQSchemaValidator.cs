using Microsoft.EntityFrameworkCore;
using System.Data;

namespace TaskFlow.Infrastructure.Data;

/// <summary>
/// Verifies TickerQ's operational tables exist without creating or changing schema.
/// </summary>
public static class TaskFlowTickerQSchemaValidator
{
    public static async Task<bool> SchemaExistsAsync(
        TaskFlowTickerQDbContext db,
        CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            // Startup validation checks only the contract Scheduler needs to run.
            // EF migrations and history-table state remain the migrator's responsibility.
            command.CommandText = $"""
                SELECT CASE WHEN
                    OBJECT_ID(N'[{TaskFlowTickerQDbContext.SchemaName}].[TimeTickers]', N'U') IS NOT NULL
    AND OBJECT_ID(N'[{TaskFlowTickerQDbContext.SchemaName}].[CronTickers]', N'U') IS NOT NULL
    AND OBJECT_ID(N'[{TaskFlowTickerQDbContext.SchemaName}].[CronTickerOccurrences]', N'U') IS NOT NULL
THEN 1 ELSE 0 END
""";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is int value && value == 1;
        }
        finally
        {
            if (closeConnection)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}
