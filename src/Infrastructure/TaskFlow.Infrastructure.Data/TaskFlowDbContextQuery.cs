using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Infrastructure.Data;

public class TaskFlowDbContextQuery : TaskFlowDbContextTrxn
{
    public TaskFlowDbContextQuery(DbContextOptions<TaskFlowDbContextQuery> options) : base(options) { }

    // Query context uses no-tracking by default (configured in RegisterServices)
    // Inherits tenant query filters and default data types from TaskFlowDbContextTrxn
}
