using EF.Domain.Contracts;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Domain.Model.Rules;

public class TaskItemStatusTransitionRule : RuleBase<(TaskItemStatus Current, TaskItemStatus Target)>
{
    private static readonly HashSet<(TaskItemStatus, TaskItemStatus)> ValidTransitions =
    [
        (TaskItemStatus.Open, TaskItemStatus.InProgress),
        (TaskItemStatus.Open, TaskItemStatus.Cancelled),
        (TaskItemStatus.InProgress, TaskItemStatus.Completed),
        (TaskItemStatus.InProgress, TaskItemStatus.Blocked),
        (TaskItemStatus.InProgress, TaskItemStatus.Cancelled),
        (TaskItemStatus.Blocked, TaskItemStatus.InProgress),
        (TaskItemStatus.Blocked, TaskItemStatus.Cancelled),
        (TaskItemStatus.Completed, TaskItemStatus.Open),
        (TaskItemStatus.Cancelled, TaskItemStatus.Open),
    ];

    public override DomainResult Evaluate((TaskItemStatus Current, TaskItemStatus Target) input)
    {
        if (input.Target == TaskItemStatus.None)
            return Pass();

        return ValidTransitions.Contains((input.Current, input.Target))
            ? Pass()
            : Fail($"Cannot transition from {input.Current} to {input.Target}.");
    }
}
