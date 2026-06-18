# TaskFlow Mobile UI Tests

MSTest + Appium smoke coverage for the Uno Android/iOS heads.

Default test runs are opt-in. Without `TASKFLOW_MOBILE_TESTS_ENABLED=true`, mobile tests report Inconclusive and exit without touching Appium.

## Prerequisites

Install these before running Android mobile UI tests:

- [Node.js](https://nodejs.org/en/download) for the Appium CLI.
- [Appium](https://appium.io/docs/en/latest/quickstart/) plus the [UiAutomator2 driver](https://appium.io/docs/en/2.0/quickstart/uiauto2-driver/).
- [Android Studio or Android SDK command-line tools](https://developer.android.com/studio), including Android SDK Platform Tools, Android Emulator, and a system image.
- An Android Virtual Device. Use Android Studio Device Manager or [`avdmanager`](https://developer.android.com/tools/avdmanager).
- Know the emulator command-line options from the [Android Emulator command-line guide](https://developer.android.com/studio/run/emulator-commandline).

Quick install/check commands:

```powershell
node --version
npm --version
npm install -g appium
appium driver install uiautomator2
appium driver list --installed
```

If Android SDK tools are not on `PATH`, use the installed SDK path directly:

```powershell
$env:ANDROID_HOME = "C:\Program Files (x86)\Android\android-sdk"
& "$env:ANDROID_HOME\emulator\emulator.exe" -list-avds
& "$env:ANDROID_HOME\platform-tools\adb.exe" devices -l
```

## Android Local Run

Build the Android package:

```powershell
dotnet restore src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj -p:BuildAllUnoTargets=true
dotnet build src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj -p:TargetFrameworkOverride=net10.0-android -p:UseMocks=true --no-restore -m:1
```

Start a visible Android emulator and wait for it to boot:

```powershell
$env:ANDROID_HOME = "C:\Program Files (x86)\Android\android-sdk"
& "$env:ANDROID_HOME\emulator\emulator.exe" -avd Android_Emulator_35 -no-snapshot-load -no-snapshot-save -no-audio -gpu swiftshader_indirect -no-boot-anim
& "$env:ANDROID_HOME\platform-tools\adb.exe" devices -l
& "$env:ANDROID_HOME\platform-tools\adb.exe" -s emulator-5554 shell getprop sys.boot_completed
```

For automation-only sessions where a visible emulator window is not needed, add `-no-window` to the emulator command.

Start Appium after the emulator reports `sys.boot_completed=1`:

```powershell
appium --address 127.0.0.1 --port 4723 --allow-insecure=uiautomator2:adb_shell
```

The `uiautomator2:adb_shell` feature is required because the test harness uses Appium's `mobile: shell` bridge to collapse the Android notification shade and route text through `adb input` when Uno/Skia text fields reject direct WebDriver `SendKeys`.

Run the mobile smoke test:

```powershell
$env:TASKFLOW_MOBILE_TESTS_ENABLED = "true"
$env:TASKFLOW_MOBILE_PLATFORM = "Android"
$env:TASKFLOW_ANDROID_APP_PATH = "src/UI/TaskFlow.Uno/bin/Debug/net10.0-android/com.taskflow.uno-Signed.apk"
dotnet test src/Test/Test.Mobile/Test.Mobile.csproj --filter TestCategory=MobileUI
```

`TASKFLOW_ANDROID_APP_PATH` and `TASKFLOW_IOS_APP_PATH` can be absolute or repo-root-relative.

The explicit restore matters. The Uno app defaults to the Wasm target for fast local web work; Android/Appium runs need `BuildAllUnoTargets=true` during restore so platform-specific Skia runtime packages, including `Uno.WinUI.Runtime.Skia.Android`, are present in the package graph.

For live Gateway/API calls, build without `-p:UseMocks=true`, keep the Gateway reachable from the emulator, and use the app's `AndroidGatewayBaseUrl` value (`10.0.2.2` for host loopback).

## Troubleshooting

- If tests say `Mobile app package not found`, rebuild the Android package and confirm `TASKFLOW_ANDROID_APP_PATH` points to the APK. Relative paths may be repo-root-relative, for example `src/UI/TaskFlow.Uno/bin/Debug/net10.0-android/com.taskflow.uno-Signed.apk`.
- If tests report Appium unavailable, check `http://127.0.0.1:4723/status` and restart Appium with `--allow-insecure=uiautomator2:adb_shell`.
- If `adb devices -l` has no `device` row, start the emulator and wait for `sys.boot_completed=1`.
- If an AVD exists but no emulator window appears, check whether it was started with `-no-window`; restart without that flag for an interactive session.
- If Android restore/build fails after a normal Wasm-only restore, run the `BuildAllUnoTargets=true` restore again before the Android build.

Known-good local readiness check:

```powershell
$env:TASKFLOW_MOBILE_TESTS_ENABLED = "true"
$env:TASKFLOW_MOBILE_PLATFORM = "Android"
$env:TASKFLOW_ANDROID_APP_PATH = "src/UI/TaskFlow.Uno/bin/Debug/net10.0-android/com.taskflow.uno-Signed.apk"
dotnet test src/Test/Test.Mobile/Test.Mobile.csproj --no-build --filter "FullyQualifiedName~TaskFlowMobile_AppLaunches_AndRendersNativeSurface"
```

## iOS Gate

iOS simulator/device execution requires macOS or a Mac host with Xcode and Appium XCUITest support. Windows can keep the MSTest project compiling, but cannot run iOS simulator/device tests locally.

```powershell
$env:TASKFLOW_MOBILE_TESTS_ENABLED = "true"
$env:TASKFLOW_MOBILE_PLATFORM = "iOS"
$env:TASKFLOW_IOS_APP_PATH = "<path-to-built-TaskFlow.Uno.app>"
dotnet test src/Test/Test.Mobile/Test.Mobile.csproj --filter TestCategory=MobileUI
```
