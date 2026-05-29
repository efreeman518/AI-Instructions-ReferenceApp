using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TaskFlow.Infrastructure.Data;

/// <summary>Provides SQL server compatibility behavior for the Infrastructure layer.</summary>
public static class SqlServerCompatibility
{
    public const int LatestCompatibilityLevel = 170;

    /// <summary>Provides the use latest compatibility level operation for SQL server compatibility.</summary>
    public static SqlServerDbContextOptionsBuilder UseLatestCompatibilityLevel(
        this SqlServerDbContextOptionsBuilder sqlOptions) =>
        sqlOptions.UseCompatibilityLevel(LatestCompatibilityLevel);

    /// <summary>Provides the use latest compatibility level operation for SQL server compatibility.</summary>
    public static AzureSqlDbContextOptionsBuilder UseLatestCompatibilityLevel(
        this AzureSqlDbContextOptionsBuilder sqlOptions) =>
        sqlOptions.UseCompatibilityLevel(LatestCompatibilityLevel);
}
