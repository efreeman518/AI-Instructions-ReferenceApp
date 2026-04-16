using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Infrastructure.Data;

public class TaskFlowDbContextQuery(DbContextOptions<TaskFlowDbContextQuery> options) : TaskFlowDbContextBase(options)
{
}
