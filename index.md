# Thermo

A lightweight, Windows-native PowerToys Command Palette extension for real-time CPU and GPU temperature monitoring. Features authentic Segoe Fluent Icons (`` microchip for CPU, `` component for GPU), clean short labels, and zero emoji clutter.

## Path & Tech Stack
- **Path**: `P:\Magnanimity\Projects\Thermo`
- **Target Framework**: `.NET 10.0` (`net10.0-windows10.0.26100.0`)
- **SDK & Libraries**:
  - `Microsoft.CommandPalette.Extensions` (v0.9.260303001)
  - `Microsoft.CommandPalette.Extensions.Toolkit`
  - `LibreHardwareMonitorLib` (v0.9.6)
  - `Shmuelie.WinRTServer` (v2.1.1)

## Displays & Formats
- **CPU Core Temp**: `CPU Core: 61°C` (Package / Core)
- **CPU Avg Temp**: `CPU Avg: 52°C` (Aggregate core average)
- **GPU Core Temp**: `GPU Core: 48°C` (NVIDIA RTX 4060 graphics core)
- **CPU Usage**: `CPU: 23%` (Win32 GetSystemTimes accurate processor load)
- **GPU Usage**: `GPU: 15%` (NVIDIA graphics 3D / core load)
- **Memory (RAM)**: `RAM: 14.1 GB` (Physical memory in use via Win32 GlobalMemoryStatusEx)
- **Individual Dock Bands**: In the Dock edit menu, all 6 metrics now appear as separate standalone items with authentic Segoe Fluent Icons (`` CPU, `` GPU, `` RAM, `` Activity), completely placeable and configurable on their own.

## File Structure Overview
```
Thermo/
├── Directory.Build.props
├── Directory.Packages.props
├── nuget.config
├── setup.iss                           # Inno Setup compiler script for x64 EXE installer
├── winget/                             # Official WinGet manifests for lupixele.Thermo
├── Install-ThermoHelper.ps1            # Auto-elevating task installer for startup helper
├── Uninstall-ThermoHelper.ps1          # Clean task uninstaller
├── Thermo/                             # Main Command Palette Extension (out-of-proc COM)
│   ├── Thermo.csproj
│   ├── app.manifest
│   ├── Package.appxmanifest            # com.microsoft.commandpalette registration
│   ├── Program.cs                      # -RegisterProcessAsComServer entry
│   ├── ThermoExtension.cs              # IExtension + COM CLSID
│   ├── ThermoCommandsProvider.cs       # Top-level command & Dock band
│   ├── Icons.cs                        # Segoe Fluent Icons glyphs (CPU \uE9D9, GPU \uE950)
│   ├── Pages/
│   │   └── TemperaturePage.cs          # ListPage with real-time in-place updates
│   ├── Services/
│   │   ├── TemperatureService.cs       # Polling engine & sensor aggregator
│   │   └── ElevatedSensorClient.cs     # Low-latency Named Pipe & fallback reader
│   └── Assets/                         # Store and square logos for MSIX packaging
└── ThermoHelper/                       # Silent elevated background helper (RunLevel: Highest)
    ├── ThermoHelper.csproj
    └── Program.cs                      # Named pipe server providing CPU Package & Average
```

## Core Goal
Provide instant, low-overhead hardware temperature visibility directly inside PowerToys Command Palette and its Dock bar, using authentic Windows Fluent design and zero emoji clutter.
