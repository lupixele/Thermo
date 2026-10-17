## Current State
Thermo extension and companion service fully implemented, compiled with .NET 10 on Windows 11, and registered with Command Palette via Windows AppModel loose packaging (`Add-AppxPackage -Register`). 

Updated to clean, concise names without any glyph placeholder boxes (`🔲`):
- `CPU Core: <temp>°C` (Package / Die)
- `CPU Avg: <temp>°C` (Core average)
- `GPU Core: <temp>°C` (NVIDIA RTX 4060)
- Summary: `CPU <temp>°C · GPU <temp>°C`

Unused display mode toggle item removed to avoid dock clutter.

## Recent Decisions
- [2026-10-17] Added Memory (RAM), CPU Usage, and GPU Usage metrics to both the in-palette ListPage (split into Temperatures and Activity & Memory sections) and as separate standalone `WrappedDockItem` widgets in the Dock edit list.
- [2026-10-17] Converted Dock bands to standalone `WrappedDockItem` instances (`thermo.hardware.cpucore`, `thermo.hardware.cpuavg`, `thermo.hardware.gpucore`) so each renders as a completely single, independent widget in the Dock Edit list.
- [2026-10-17] Split Dock bands into individual independent items: `CPU Core`, `CPU Avg`, `GPU Core`.
- [2026-10-17] Removed in-string unicode glyphs that were causing square `🔲` tofu box artifacts in standard UI fonts.
- [2026-10-17] Shortened all labels to compact Dock-friendly text (`CPU: <temp>`, `CPU Avg: <temp>`, `GPU: <temp>`).
- [2026-10-17] Name finalized as `Thermo`.
- [2026-10-17] Native Windows Segoe Fluent Icons (`` CPU microchip with pins, `` GPU component) attached directly to items as native icon objects.
- [2026-10-17] Implemented `ThermoHelper` background service using high-performance Named Pipe IPC + fallback file, registered as `ThermoHardwareHelper` Scheduled Task (RunLevel: Highest).

## Next Steps
All implementation and refinements complete. Extension running in Command Palette.

## Open Questions / Blockers
None.

## Last Updated
2026-10-17
