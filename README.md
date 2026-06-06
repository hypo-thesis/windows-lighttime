# BrightTime

A portable Windows 11/10 system tray app that **automatically reduces real monitor brightness** based on the time of day. No blue-light filtering, no color temperature changes — just pure brightness control.

## Features

- **Time-based automatic schedule** — brightness changes throughout the day via configurable schedule points with smooth interpolation
- **WMI** — controls internal laptop display backlight
- **DDC/CI** — controls external monitor brightness via `dxva2.dll`
- **Overlay fallback** — transparent click-through overlay if hardware control fails
- **Manual override** — slider to set brightness temporarily (auto-returns after 30 min)
- **Smooth transitions** — gradual brightness changes between schedule points
- **System tray** — right-click menu for quick access (Show, Auto toggle, quick brightness presets, Exit)
- **Close-to-tray** — closing the window hides to tray; only Exit terminates
- **Single instance** — second launch brings existing window to front
- **Start with Windows** — toggle via checkbox (writes HKCU registry)
- **Portable** — single EXE, no install, no admin required
- **Persistent settings** — saved to `%AppData%\BrightTime\settings.json`
- **Error logging** — details written to `%AppData%\BrightTime\logs\brighttime.log`

## What it does NOT do

- Does **not** use blue-light filters (Night Light, f.lux, etc.)
- Does **not** change color temperature, gamma, or white balance
- Does **not** require administrator privileges
- Does **not** collect telemetry or usage data
- Does **not** require installation

## Hardware brightness vs. overlay dimming

| Method | Backlight reduction | Power savings |
|--------|:-------------------:|:-------------:|
| **WMI** (internal display) | ✅ Yes | ✅ Yes |
| **DDC/CI** (external monitor) | ✅ Yes | ✅ Yes |
| **Overlay** (software fallback) | ❌ No | ❌ No |

Hardware brightness physically reduces the monitor backlight. Overlay dimming places a transparent black window over the screen — it lowers perceived brightness but does not save power.

## Download

Grab the latest portable EXE from the [Releases](https://github.com/hypo-thesis/windows-lighttime/releases) page.

**System requirements:** Windows 10 or Windows 11, 64-bit.

## Quick start

1. Download `BrightTime.exe`
2. Double-click to launch
3. App appears in the system tray (notification area)
4. Right-click the tray icon for the menu
5. Double-click the tray icon to open the settings window

## Tray menu

| Item | Action |
|------|--------|
| **Show** | Opens the settings window |
| **Enable/Disable Automatic Brightness** | Toggles the schedule |
| **Set Brightness X%** | Immediately sets brightness |
| **Restore Previous Brightness** | Restores the brightness from before the app started |
| **Exit** | Fully closes the app |

## Default schedule

| Time | Brightness |
|------|:----------:|
| 07:00 | 100% |
| 12:00 | 90% |
| 18:00 | 70% |
| 21:00 | 45% |
| 23:30 | 25% |

Brightness is interpolated between points. At 19:30, the target is ~57%.

## Building from source

**Prerequisites:** .NET 8 SDK

```powershell
git clone https://github.com/hypo-thesis/windows-lighttime.git
cd windows-lighttime
dotnet publish -c Release -r win-x64 `
  /p:PublishSingleFile=true `
  /p:SelfContained=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:PublishReadyToRun=true `
  /p:EnableCompressionInSingleFile=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\BrightTime.exe`

## Settings

- **Settings file:** `%AppData%\BrightTime\settings.json`
- **Log file:** `%AppData%\BrightTime\logs\brighttime.log`

Delete the settings file to reset to defaults.

## Known limitations

- Some external monitors do **not** support DDC/CI brightness control
- Some monitors have DDC/CI **disabled** in the on-screen display menu (check monitor settings)
- Overlay fallback dims the screen visually but does **not** reduce real backlight or save power
- WMI brightness APIs may behave differently depending on laptop brand and driver support
- DDC/CI may not work over some DisplayPort hubs or USB-C adapters

## License

MIT
