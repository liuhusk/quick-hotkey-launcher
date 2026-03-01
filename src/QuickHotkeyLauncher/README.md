# QuickHotkeyLauncher

A lightweight local Windows tool to bind global hotkeys to applications.

## Features

- Add app from installed app catalog (Start Menu + Registry)
- Add custom executable path (`.exe`)
- Remove binding
- Reset hotkey
- Global hotkey behavior:
  - If target app is not running: launch it
  - If target app is running: bring its window to foreground
- Local JSON config persistence
- Single instance guard

## Runtime

- Windows 10 / Windows 11
- .NET 8 SDK (for build)

## Build and Run

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

## Configuration File

Stored at:

`%LocalAppData%\QuickHotkeyLauncher\config.json`

## Project Structure

- `Program.cs`: app entry and single instance mutex
- `Forms/MainForm.cs`: main UI and hotkey message dispatch
- `Forms/AddAppForm.cs`: add binding dialog
- `Forms/HotkeyCaptureForm.cs`: hotkey capture dialog
- `Services/HotkeyService.cs`: global hotkey register/unregister
- `Services/LaunchFocusService.cs`: launch or focus window
- `Services/InstalledAppCatalogService.cs`: installed app discovery
- `Services/ConfigService.cs`: JSON load/save
- `Models/*`: config and entity models

## Notes for Next Iteration

- Add tray mode and startup with Windows
- Add icon extraction for app list
- Add conflict details tooltip in grid
- Add unit tests for config and hotkey conflict checks
