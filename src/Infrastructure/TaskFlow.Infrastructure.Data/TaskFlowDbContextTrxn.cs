using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Infrastructure.Data;

public class TaskFlowDbContextTrxn(DbContextOptions<TaskFlowDbContextTrxn> options) : TaskFlowDbContextBase(options)
{
}
