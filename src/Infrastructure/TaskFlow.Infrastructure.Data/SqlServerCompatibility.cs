using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TaskFlow.Infrastructure.Data;

public static class SqlServerCompatibility
{
    public const int LatestCompatibilityLevel = 170;

    public static SqlServerDbContextOptionsBuilder UseLatestCompatibilityLevel(
        this SqlServerDbContextOptionsBuilder sqlOptions) =>
        sqlOptions.UseCompatibilityLevel(LatestCompatibilityLevel);

    public static AzureSqlDbContextOptionsBuilder UseLatestCompatibilityLevel(
        this AzureSqlDbContextOptionsBuilder sqlOptions) =>
        sqlOptions.UseCompatibilityLevel(LatestCompatibilityLevel);
}
