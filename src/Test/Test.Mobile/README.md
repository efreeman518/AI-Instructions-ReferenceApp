# TaskFlow Mobile UI Tests

MSTest + Appium smoke coverage for the Uno Android/iOS heads.

Default test runs are opt-in. Without `TASKFLOW_MOBILE_TESTS_ENABLED=true`, the mobile smoke test writes the setup command and exits without touching Appium.

## Android Local Run

Build the Android package:

```powershell
dotnet restore src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj -p:BuildAllUnoTargets=true
dotnet build src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj -p:TargetFrameworkOverride=net10.0-android -p:UseMocks=true --no-restore -m:1
```

Start an Android emulator and Appium server with the UiAutomator2 driver installed:

```powershell
npm install -g appium
appium driver install uiautomator2
appium driver doctor uiautomator2
appium
```

Run the mobile smoke test:

```powershell
$env:TASKFLOW_MOBILE_TESTS_ENABLED = "true"
$env:TASKFLOW_MOBILE_PLATFORM = "Android"
$env:TASKFLOW_ANDROID_APP_PATH = "src/UI/TaskFlow.Uno/bin/Debug/net10.0-android/com.taskflow.uno-Signed.apk"
dotnet test src/Test/Test.Mobile/Test.Mobile.csproj --filter TestCategory=MobileUI
```

The explicit restore matters. The Uno app defaults to the Wasm target for fast local web work; Android/Appium runs need `BuildAllUnoTargets=true` during restore so platform-specific Skia runtime packages, including `Uno.WinUI.Runtime.Skia.Android`, are present in the package graph.

For live Gateway/API calls, build without `-p:UseMocks=true`, keep the Gateway reachable from the emulator, and use the app's `AndroidGatewayBaseUrl` value (`10.0.2.2` for host loopback).

## iOS Gate

iOS simulator/device execution requires macOS or a Mac host with Xcode and Appium XCUITest support. Windows can keep the MSTest project compiling, but cannot run iOS simulator/device tests locally.

```powershell
$env:TASKFLOW_MOBILE_TESTS_ENABLED = "true"
$env:TASKFLOW_MOBILE_PLATFORM = "iOS"
$env:TASKFLOW_IOS_APP_PATH = "<path-to-built-TaskFlow.Uno.app>"
dotnet test src/Test/Test.Mobile/Test.Mobile.csproj --filter TestCategory=MobileUI
```
