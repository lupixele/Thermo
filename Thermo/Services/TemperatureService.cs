using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace Thermo.Services;

public class HardwareReading
{
    // Temperatures
    public float? CpuPackage { get; set; }
    public float? CpuAverage { get; set; }
    public float? GpuCore { get; set; }
    public bool IsCpuHelperConnected { get; set; }

    // Usages
    public float? CpuUsage { get; set; }
    public float? GpuUsage { get; set; }
    public float? MemoryUsagePercent { get; set; }
    public float? MemoryUsedGb { get; set; }
    public float? MemoryTotalGb { get; set; }

    // Formatters
    public string FormatCpuPackage() => CpuPackage.HasValue ? $"{Math.Round(CpuPackage.Value)}°C" : "--";
    public string FormatCpuAverage() => CpuAverage.HasValue ? $"{Math.Round(CpuAverage.Value)}°C" : "--";
    public string FormatGpuCore() => GpuCore.HasValue ? $"{Math.Round(GpuCore.Value)}°C" : "--";

    public string FormatCpuUsage() => CpuUsage.HasValue ? $"{Math.Round(CpuUsage.Value)}%" : "--";
    public string FormatGpuUsage() => GpuUsage.HasValue ? $"{Math.Round(GpuUsage.Value)}%" : "--";
    public string FormatMemoryUsage() => MemoryUsagePercent.HasValue ? $"{Math.Round(MemoryUsagePercent.Value)}%" : "--";
    public string FormatMemoryDock() => MemoryUsedGb.HasValue ? $"{MemoryUsedGb.Value:0.#} GB" : "--";
    public string FormatMemoryDetailed() =>
        (MemoryUsedGb.HasValue && MemoryTotalGb.HasValue)
            ? $"{FormatMemoryUsage()} ({MemoryUsedGb.Value:0.#}/{MemoryTotalGb.Value:0.#} GB)"
            : FormatMemoryUsage();

    public string GetSummaryString()
    {
        string cpuStr = CpuPackage.HasValue ? $"{Math.Round(CpuPackage.Value)}°C" : "--";
        string gpuStr = GpuCore.HasValue ? $"{Math.Round(GpuCore.Value)}°C" : "--";
        string memStr = MemoryUsagePercent.HasValue ? $"{Math.Round(MemoryUsagePercent.Value)}%" : "--";
        return $"CPU {cpuStr} · GPU {gpuStr} · RAM {memStr}";
    }
}

public sealed class TemperatureService : IDisposable
{
    private static TemperatureService? _instance;
    private static readonly object _sync = new();

    private readonly Computer? _computer;
    private HardwareReading _currentReading = new();
    private readonly object _readingLock = new();

    // CPU Usage tracking via GetSystemTimes
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    private long _prevIdleTime;
    private long _prevKernelTime;
    private long _prevUserTime;

    // Memory tracking via GlobalMemoryStatusEx
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    public static TemperatureService Instance
    {
        get
        {
            lock (_sync)
            {
                return _instance ??= new TemperatureService();
            }
        }
    }

    private TemperatureService()
    {
        try
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false,
                IsMotherboardEnabled = false,
                IsStorageEnabled = false,
                IsNetworkEnabled = false,
                IsControllerEnabled = false,
                IsBatteryEnabled = false,
                IsPsuEnabled = false
            };
            _computer.Open();
        }
        catch
        {
            _computer = null;
        }

        // Prime the initial system times
        GetSystemTimes(out _prevIdleTime, out _prevKernelTime, out _prevUserTime);
    }

    public HardwareReading CurrentReading
    {
        get
        {
            lock (_readingLock)
            {
                return _currentReading;
            }
        }
    }

    public async Task<HardwareReading> RefreshAsync()
    {
        float? gpuTemp = null;
        float? gpuLoad = null;
        float? directCpuPkg = null;
        float? directCpuAvg = null;
        float? directCpuLoad = null;

        // 1. Direct hardware sample (GPU Temp/Load & CPU fallback)
        if (_computer != null)
        {
            try
            {
                foreach (var hw in _computer.Hardware)
                {
                    hw.Update();

                    if (hw.HardwareType == HardwareType.GpuNvidia ||
                        hw.HardwareType == HardwareType.GpuAmd ||
                        hw.HardwareType == HardwareType.GpuIntel)
                    {
                        foreach (var s in hw.Sensors)
                        {
                            if (s.Value.HasValue)
                            {
                                if (s.SensorType == SensorType.Temperature)
                                {
                                    if (string.Equals(s.Name, "GPU Core", StringComparison.OrdinalIgnoreCase))
                                    {
                                        gpuTemp = s.Value.Value;
                                    }
                                    else if (!gpuTemp.HasValue && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                                    {
                                        gpuTemp = s.Value.Value;
                                    }
                                }
                                else if (s.SensorType == SensorType.Load)
                                {
                                    if (string.Equals(s.Name, "GPU Core", StringComparison.OrdinalIgnoreCase))
                                    {
                                        gpuLoad = s.Value.Value;
                                    }
                                }
                            }
                        }
                    }
                    else if (hw.HardwareType == HardwareType.Cpu)
                    {
                        foreach (var s in hw.Sensors)
                        {
                            if (s.Value.HasValue)
                            {
                                if (s.SensorType == SensorType.Temperature)
                                {
                                    if (string.Equals(s.Name, "CPU Package", StringComparison.OrdinalIgnoreCase))
                                    {
                                        directCpuPkg = s.Value.Value;
                                    }
                                    else if (string.Equals(s.Name, "Core Average", StringComparison.OrdinalIgnoreCase))
                                    {
                                        directCpuAvg = s.Value.Value;
                                    }
                                }
                                else if (s.SensorType == SensorType.Load)
                                {
                                    if (string.Equals(s.Name, "CPU Total", StringComparison.OrdinalIgnoreCase))
                                    {
                                        directCpuLoad = s.Value.Value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        // 2. Query elevated helper for accurate CPU MSR reading
        var helperData = await ElevatedSensorClient.TryReadAsync();

        // 3. Compute CPU Usage from Win32 GetSystemTimes
        float cpuUsage = directCpuLoad ?? 0;
        if (GetSystemTimes(out long idleTime, out long kernelTime, out long userTime))
        {
            long diffIdle = idleTime - _prevIdleTime;
            long diffKernel = kernelTime - _prevKernelTime;
            long diffUser = userTime - _prevUserTime;
            long total = diffKernel + diffUser;

            _prevIdleTime = idleTime;
            _prevKernelTime = kernelTime;
            _prevUserTime = userTime;

            if (total > 0)
            {
                float calculated = (float)(total - diffIdle) / total * 100.0f;
                cpuUsage = Math.Clamp(calculated, 0.0f, 100.0f);
            }
        }

        // 4. Compute Memory Usage from Win32 GlobalMemoryStatusEx
        float memPercent = 0;
        float memUsedGb = 0;
        float memTotalGb = 0;
        var memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
        {
            memPercent = memStatus.dwMemoryLoad;
            memTotalGb = (float)memStatus.ullTotalPhys / (1024 * 1024 * 1024);
            memUsedGb = (float)(memStatus.ullTotalPhys - memStatus.ullAvailPhys) / (1024 * 1024 * 1024);
        }

        var newReading = new HardwareReading
        {
            GpuCore = gpuTemp,
            GpuUsage = gpuLoad,
            CpuPackage = helperData != null ? helperData.CpuPackage : directCpuPkg,
            CpuAverage = helperData != null ? helperData.CpuAverage : directCpuAvg,
            CpuUsage = cpuUsage,
            MemoryUsagePercent = memPercent,
            MemoryUsedGb = memUsedGb,
            MemoryTotalGb = memTotalGb,
            IsCpuHelperConnected = helperData != null
        };

        lock (_readingLock)
        {
            _currentReading = newReading;
        }

        return newReading;
    }

    public static void InstallElevationService()
    {
        try
        {
            var appDir = AppContext.BaseDirectory;
            var helperExe = Path.Combine(appDir, "ThermoHelper.exe");
            if (!File.Exists(helperExe))
            {
                var devHelper = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "ThermoHelper", "bin", "Debug", "net10.0-windows10.0.26100.0", "win-x64", "ThermoHelper.exe"));
                if (File.Exists(devHelper))
                {
                    helperExe = devHelper;
                }
            }

            var scriptPath = Path.Combine(Path.GetTempPath(), "thermo-setup.ps1");
            var script = $@"
$action = New-ScheduledTaskAction -Execute '{helperExe.Replace("'", "''")}'
$trigger = New-ScheduledTaskTrigger -AtLogOn
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero)
Register-ScheduledTask -TaskName 'ThermoHardwareHelper' -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force
Start-ScheduledTask -TaskName 'ThermoHardwareHelper'
";
            File.WriteAllText(scriptPath, script);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        try
        {
            _computer?.Close();
        }
        catch
        {
        }
    }
}
