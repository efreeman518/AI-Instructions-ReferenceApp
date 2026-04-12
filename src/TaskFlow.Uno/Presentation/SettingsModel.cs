namespace TaskFlow.Uno.Presentation;

public partial record SettingsModel(INavigator Navigator)
{
    public IState<bool> UseMocks => State<bool>.Value(this, () => false);

    public IState<string> GatewayUrl => State<string>.Value(this, () => "https://localhost:7200");

    public async ValueTask NavigateBack(CancellationToken ct) =>
        await Navigator.NavigateBackAsync(this, cancellation: ct);
}
