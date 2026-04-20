using TaskFlow.Uno.Core.Business.Notifications;

namespace Test.Unit.Uno;

[TestClass]
[TestCategory("Unit")]
[TestCategory("Uno")]
public class NotificationServiceTests
{
    [TestMethod]
    public async Task ShowSuccess_Appends_Item()
    {
        var svc = new NotificationService();
        await svc.ShowSuccess("Saved.", "Done");

        Assert.AreEqual(1, svc.Items.Count);
        Assert.AreEqual(NotificationSeverity.Success, svc.Items[0].Severity);
        Assert.AreEqual("Saved.", svc.Items[0].Message);
        Assert.AreEqual("Done", svc.Items[0].Title);
    }

    [TestMethod]
    public async Task ShowError_Persists_NoAutoDismiss()
    {
        var svc = new NotificationService();
        await svc.ShowError("Boom");

        Assert.AreEqual(1, svc.Items.Count);
        Assert.IsNull(svc.Items[0].AutoDismissAfter);
    }

    [TestMethod]
    public async Task ShowProblem_Uses_ProblemTitle_And_Detail()
    {
        var svc = new NotificationService();
        var problem = new ProblemDetailsPayload
        {
            Status = 400,
            Title = "Validation failed",
            Detail = "Name is required"
        };

        await svc.ShowProblem(problem);

        Assert.AreEqual(1, svc.Items.Count);
        var n = svc.Items[0];
        Assert.AreEqual(NotificationSeverity.Error, n.Severity);
        Assert.AreEqual("Validation failed", n.Title);
        Assert.AreEqual("Name is required", n.Message);
        Assert.AreEqual("400:Validation failed", n.DedupeKey);
    }

    [TestMethod]
    public async Task ShowProblem_Dedupes_By_Status_And_Title()
    {
        var svc = new NotificationService();
        var p = new ProblemDetailsPayload { Status = 500, Title = "Oops", Detail = "first" };
        var q = new ProblemDetailsPayload { Status = 500, Title = "Oops", Detail = "second" };

        await svc.ShowProblem(p);
        await svc.ShowProblem(q);
        await svc.ShowProblem(q);

        Assert.AreEqual(1, svc.Items.Count);
        Assert.AreEqual("second", svc.Items[0].Message);
    }

    [TestMethod]
    public async Task Dismiss_Removes_Item_By_Id()
    {
        var svc = new NotificationService();
        await svc.ShowError("first");
        await svc.ShowError("second");

        var idToKeep = svc.Items[0].Id;
        var idToRemove = svc.Items[1].Id;

        await svc.Dismiss(idToRemove);

        Assert.AreEqual(1, svc.Items.Count);
        Assert.AreEqual(idToKeep, svc.Items[0].Id);
    }

    [TestMethod]
    public async Task Dismiss_Unknown_Id_IsNoOp()
    {
        var svc = new NotificationService();
        await svc.ShowError("x");

        await svc.Dismiss(Guid.NewGuid());

        Assert.AreEqual(1, svc.Items.Count);
    }
}
