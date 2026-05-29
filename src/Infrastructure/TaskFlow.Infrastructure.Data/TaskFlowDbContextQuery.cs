using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Infrastructure.Data;

/// <summary>Carries task flow DB context query CQRS data between endpoints and handlers.</summary>
public class TaskFlowDbContextQuery(DbContextOptions<TaskFlowDbContextQuery> options) : TaskFlowDbContextBase(options)
{
}
