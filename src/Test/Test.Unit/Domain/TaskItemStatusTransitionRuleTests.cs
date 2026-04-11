using TaskFlow.Domain.Model.Rules;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Unit.Domain;

[TestClass]
public class TaskItemStatusTransitionRuleTests
{
    private readonly TaskItemStatusTransitionRule _rule = new();

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(TaskItemStatus.Open, TaskItemStatus.InProgress)]
    [DataRow(TaskItemStatus.Open, TaskItemStatus.Cancelled)]
    [DataRow(TaskItemStatus.InProgress, TaskItemStatus.Completed)]
    [DataRow(TaskItemStatus.InProgress, TaskItemStatus.Blocked)]
    [DataRow(TaskItemStatus.InProgress, TaskItemStatus.Cancelled)]
    [DataRow(TaskItemStatus.Blocked, TaskItemStatus.InProgress)]
    [DataRow(TaskItemStatus.Blocked, TaskItemStatus.Cancelled)]
    [DataRow(TaskItemStatus.Completed, TaskItemStatus.Open)]
    [DataRow(TaskItemStatus.Cancelled, TaskItemStatus.Open)]
    public void Given_ValidTransition_When_Evaluated_Then_Passes(TaskItemStatus current, TaskItemStatus target)
    {
        var result = _rule.Evaluate((current, target));
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(TaskItemStatus.Open, TaskItemStatus.Completed)]
    [DataRow(TaskItemStatus.Open, TaskItemStatus.Blocked)]
    [DataRow(TaskItemStatus.Completed, TaskItemStatus.InProgress)]
    [DataRow(TaskItemStatus.Completed, TaskItemStatus.Cancelled)]
    [DataRow(TaskItemStatus.Cancelled, TaskItemStatus.InProgress)]
    [DataRow(TaskItemStatus.Cancelled, TaskItemStatus.Completed)]
    [DataRow(TaskItemStatus.Blocked, TaskItemStatus.Completed)]
    [DataRow(TaskItemStatus.Blocked, TaskItemStatus.Open)]
    public void Given_InvalidTransition_When_Evaluated_Then_Fails(TaskItemStatus current, TaskItemStatus target)
    {
        var result = _rule.Evaluate((current, target));
        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Given_TransitionToNone_When_Evaluated_Then_Passes()
    {
        var result = _rule.Evaluate((TaskItemStatus.Open, TaskItemStatus.None));
        Assert.IsTrue(result.IsSuccess);
    }
}
