using Android.Runtime;

namespace TaskFlow.Uno.Droid;

/// <summary>Provides platform entry behavior for the Uno application target.</summary>
[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/AppTheme"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    /// <summary>Initializes application with required dependencies and default state.</summary>
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }
}
