using System;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Thermo
{
    internal static class Icons
    {
        // CPU: Microchip with pins (\uE9D9)
        internal static IconInfo CpuIcon => new("\uE9D9");

        // GPU: Hardware component (\uE950)
        internal static IconInfo GpuIcon => new("\uE950");

        // Memory / RAM: RAM module glyph (\uE964)
        internal static IconInfo MemoryIcon => new("\uE964");

        // Activity / Usage pulse graph (\uE9D2)
        internal static IconInfo ActivityIcon => new("\uE9D2");

        // Thermo: Processor / Thermal icon (\uE9CA)
        internal static IconInfo ThermoIcon => new("\uE9CA");

        // Shield: Admin / Shield (\uE7EF)
        internal static IconInfo ShieldIcon => new("\uE7EF");
    }
}
