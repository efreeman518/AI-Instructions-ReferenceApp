using Microsoft.Extensions.Configuration;

namespace TaskFlow.Uno.Presentation.Presentation;

/// <summary>Drives settings state, navigation, and commands for the Uno presentation layer.</summary>
public partial record SettingsModel(INavigator Navigator, IConfiguration Configuration)
{
    public IState<bool> UseMocks => State<bool>.Value(this, () => false);

    public IState<string> GatewayUrl => State<string>.Value(this, () => Configuration["GatewayBaseUrl"] ?? string.Empty);
}
