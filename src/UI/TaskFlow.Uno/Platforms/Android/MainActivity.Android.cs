using Android.App;
using Android.Views;

namespace TaskFlow.Uno.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
/// <summary>Provides platform entry behavior for the Uno main target.</summary>
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
}
