# Windows Margin Manager

A C# utility that provides a global hotkey to resize all open windows to nearly fullscreen with configurable margins.

## Features

- Global hotkey registration (default: Ctrl+Alt+M)
- Configurable margins (pixels or percentage)
- Affects all top-level windows
- System tray integration
- Lightweight and efficient

## Requirements

- Windows 10/11
- .NET 6.0 or later

## Usage

1. Run the application
2. Configure margins in the system tray menu
3. Press Ctrl+Alt+M to apply margins to all windows
4. Press the hotkey again to restore original window positions

## Configuration

Margins can be set as:
- Fixed pixels (e.g., 50px from each edge)
- Percentage of screen (e.g., 5% from each edge)

## Building

```bash
dotnet build
dotnet run
```

## License

MIT License
