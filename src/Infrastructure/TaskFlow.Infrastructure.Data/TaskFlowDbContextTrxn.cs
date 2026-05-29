using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Infrastructure.Data;

/// <summary>Provides task flow DB context trxn behavior for the Infrastructure layer.</summary>
public class TaskFlowDbContextTrxn(DbContextOptions<TaskFlowDbContextTrxn> options) : TaskFlowDbContextBase(options)
{
}
