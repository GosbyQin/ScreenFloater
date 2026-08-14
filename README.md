# ScreenFloater

ScreenFloater is a small portable Windows tool for previewing an extended monitor in a floating window on your main screen.

It is designed for cases where a computer is connected to a large external display as an extended screen, but the operator cannot directly see that external display.

## Features

- Live preview of another monitor.
- Portable Windows executable, no installation required.
- Defaults to the first non-primary monitor, usually the external extended display.
- Resizable floating preview window.
- Display mode selector:
  - `Fill`: fills the preview area and reduces black bars.
  - `Full`: keeps the whole source screen visible with original aspect ratio.
- Work area capture mode to exclude the taskbar or system-reserved area, which helps remove bottom black bars caused by the extended screen taskbar.
- Always-on-top toggle.
- Pause/resume preview refresh.
- Monitor refresh button for display plug/unplug changes.
- Enlarged mouse preview with:
  - yellow locator ring
  - larger cursor overlay
  - zoom lens around the cursor area

## Download / Run

Use the executable in this folder:

```text
dist\ScreenFloater-x64.exe
```

Double-click it to run.

The app is a local unsigned executable, so Windows SmartScreen may show a warning the first time it runs. If you trust this local build, click:

```text
More info -> Run anyway
```

## Basic Usage

1. Connect your external display and set Windows display mode to `Extend`.
2. Run `dist\ScreenFloater-x64.exe`.
3. The app will try to preview the first non-primary display automatically.
4. Use the monitor dropdown at the top to switch screens if needed.
5. Resize the window by dragging its edges or corners.
6. Keep `Top` enabled if you want the preview window to stay above other windows.

## Toolbar

| Control | Description |
| --- | --- |
| Monitor dropdown | Selects which screen to preview. |
| `Fill` / `Full` | Switches preview display mode. `Fill` fills the window; `Full` keeps the whole screen visible with original aspect ratio. |
| `Work` | Captures only the screen working area, usually excluding the taskbar and reserved desktop area. |
| `Top` | Keeps the preview window always on top. |
| `Pause` | Temporarily stops preview refresh. |
| `Mouse+` | Shows enlarged mouse locator and zoom preview when the cursor is on the previewed screen. |
| `Refresh` | Rescans connected monitors. Use this after plugging or unplugging displays. |

## Hotkeys

| Key | Action |
| --- | --- |
| `T` | Toggle always-on-top. |
| `Space` | Pause/resume preview refresh. |
| `R` | Refresh monitor list. |
| `M` | Toggle mouse magnifier preview. |

## Mouse+ Mode

`Mouse+` helps locate the mouse on a large external screen.

When the cursor is on the screen being previewed, ScreenFloater draws:

- a yellow locator ring around the cursor position
- a larger cursor overlay
- a zoom lens near the bottom corner

If the cursor is near the bottom-right corner, the zoom lens moves to the bottom-left corner to avoid covering the cursor area.

## Notes And Limitations

- ScreenFloater uses Windows screen capture APIs.
- DRM-protected video may appear black in the preview because of system or content protection.
- The app is intended for ordinary desktop, browser, presentation, and application preview use.
- If multiple monitors use different Windows scaling settings, this version uses per-monitor DPI awareness to reduce cropped or partial preview issues.
- Very high-resolution screens may use more CPU/GPU during live preview.

## Troubleshooting

### The preview shows the wrong screen

Use the monitor dropdown at the top and select another display.

### The monitor list looks outdated

Click `Refresh`, or press `R`, after plugging or unplugging a display.

### The preview is black for a video

The content may be DRM-protected. Try previewing normal desktop content, a browser page without protected playback, or a presentation window.

### The preview looks incomplete or cropped

Check the source size shown in the toolbar:

```text
Source: WIDTHxHEIGHT
```

Compare it with the actual resolution of the external display in Windows Display Settings. If they do not match, rebuild or adjust the capture mode for that display configuration.

### The mouse magnifier is distracting

Turn off `Mouse+` in the toolbar, or press `M`.

## Files

| File | Purpose |
| --- | --- |
| `dist/ScreenFloater-x64.exe` | Main portable Windows executable. |
| `src/ScreenFloater/Program.cs` | C# WinForms source code. |
| `build.ps1` | PowerShell build script. |
| `README.md` | Full usage guide. |
| `README.zh-CN.md` | Chinese usage guide. |

## Version

Current build:

```text
dist\ScreenFloater-x64.exe
```

Main changes in this version:

- Changed toolbar labels back to Chinese in the app.
- Added preview display mode selector.
- Defaults to fill mode to reduce black bars when the window aspect ratio does not match the source screen.
- Added work area capture mode to remove taskbar/reserved-area black bars.
- Added mouse magnifier preview.
- Added larger cursor overlay and yellow locator ring.
- Kept per-monitor DPI awareness from the previous fix.
