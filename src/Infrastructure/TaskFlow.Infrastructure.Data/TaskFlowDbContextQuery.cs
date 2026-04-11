using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Infrastructure.Data;

public class TaskFlowDbContextQuery : TaskFlowDbContextTrxn
{
    public TaskFlowDbContextQuery(DbContextOptions<TaskFlowDbContextQuery> options) : base(options) { }

    // Query context uses no-tracking by default
    // Phase 5a: ConnectionNoLockInterceptor will be added
}
