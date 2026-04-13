using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskFlow.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaskFlowDbContextTrxn>
{
    public TaskFlowDbContextTrxn CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowDbContextTrxn>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TaskFlow_Design;Trusted_Connection=True;");
        return new TaskFlowDbContextTrxn(optionsBuilder.Options) { AuditId = "design-time" };
    }
}
