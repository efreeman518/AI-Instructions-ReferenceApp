namespace TaskFlow.Uno.Core.Business.Notifications;

public interface IUiDispatcher
{
    bool HasThreadAccess { get; }
    void Post(Action action);

    public static IUiDispatcher Inline { get; } = new InlineDispatcher();

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool HasThreadAccess => true;
        public void Post(Action action) => action();
    }
}
