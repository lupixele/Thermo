# Thermo 🌡️

> A clean, native Windows 11 hardware monitor extension for **PowerToys Command Palette** and the **PowerToys Dock**.

Monitors CPU temperatures, GPU temperature, and system utilization with authentic Segoe Fluent Icons, zero emoji clutter, and independent dock widgets.

---

## Features

- **Windows 11 Fluent Design**: Uses genuine Segoe Fluent Icons (`` CPU microchip with pins, `` GPU component, `` RAM module, `` Activity pulse graph).
- **Independent Dock Modules**: In the Command Palette Dock edit menu, every metric appears as its own standalone button that can be enabled, disabled, and repositioned independently:
  - `CPU Core`: Die / Package temperature
  - `CPU Avg`: Aggregate average across all active CPU cores
  - `GPU Core`: Graphics core temperature (NVIDIA / AMD / Intel)
  - `CPU Usage`: Real-time processor load via Win32 `GetSystemTimes`
  - `GPU Usage`: Graphics 3D engine load
  - `Memory (RAM)`: Physical RAM in use and percentage via Win32 `GlobalMemoryStatusEx`
- **Zero-UAC Silent Helper**: CPU Model-Specific Registers (MSR) require administrative privileges on modern Intel/AMD chips. Thermo includes a background helper (`ThermoHelper`) that runs silently via Windows Scheduled Task (`RunLevel: Highest`), eliminating annoying UAC popups every time you open Command Palette.

---

## Installation

### Prerequisites
- Windows 11 (22H2 or newer)
- [PowerToys](https://github.com/microsoft/PowerToys) with Command Palette enabled (`Win + Alt + Space`)
- [PawnIO](https://github.com/namazso/PawnIO) driver (`winget install namazso.PawnIO`) for CPU MSR reading

### Install via WinGet
```bash
winget install lupixele.Thermo
```
*(Pending community manifest approval on microsoft/winget-pkgs)*

### Local Development Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/lupixele/Thermo.git
   cd Thermo
   ```
2. Build the solution (.NET 10 SDK required):
   ```bash
   dotnet build Thermo.sln -c Debug -p:Platform=x64
   ```
3. Register the extension with Windows:
   ```powershell
   Add-AppxPackage -Register .\Thermo\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\AppxManifest.xml
   ```
4. Install the silent elevated CPU helper:
   ```powershell
   .\Install-ThermoHelper.ps1
   ```
5. Press `Win + Alt + Space`, search `Thermo`, and press Enter!

---

## Architecture

- **Extension (`Thermo`)**: Out-of-process COM server implementing `Microsoft.CommandPalette.Extensions` (`IExtension`, `ICommandProvider3`). Targets `net10.0-windows10.0.26100.0` with WinUI MSIX loose deployment.
- **Companion Service (`ThermoHelper`)**: Standalone background daemon reading CPU MSR registers via `LibreHardwareMonitorLib` and serving data to `Thermo` over named pipe `ThermoCpuPipe` with microsecond latency.

---

## License

[MIT License](LICENSE) © 2026 lupixele
