// Copyright (c) Microsoft Corporation and Thermo contributors. MIT license.

using System;
using System.Timers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Thermo.Services;

namespace Thermo.Pages;

internal sealed partial class TemperaturePage : ListPage, IDisposable
{
    // Temperature items
    private readonly ListItem _cpuPkgItem = new(new NoOpCommand());
    private readonly ListItem _cpuAvgItem = new(new NoOpCommand());
    private readonly ListItem _gpuItem = new(new NoOpCommand());

    // Usage & Memory items
    private readonly ListItem _cpuUsageItem = new(new NoOpCommand());
    private readonly ListItem _gpuUsageItem = new(new NoOpCommand());
    private readonly ListItem _memItem = new(new NoOpCommand());

    private readonly ListItem _enableHelperItem;

    private readonly Timer _timer;
    private bool _disposed;

    public TemperaturePage()
    {
        Icon = Icons.ThermoIcon;
        Title = "Hardware Monitor";
        Name = "Hardware Monitor";

        // Temperature Section
        _cpuPkgItem.Icon = Icons.CpuIcon;
        _cpuPkgItem.Section = "Temperatures";

        _cpuAvgItem.Icon = Icons.CpuIcon;
        _cpuAvgItem.Section = "Temperatures";

        _gpuItem.Icon = Icons.GpuIcon;
        _gpuItem.Section = "Temperatures";

        // Usage & Memory Section
        _cpuUsageItem.Icon = Icons.ActivityIcon;
        _cpuUsageItem.Section = "Activity & Memory";

        _gpuUsageItem.Icon = Icons.ActivityIcon;
        _gpuUsageItem.Section = "Activity & Memory";

        _memItem.Icon = Icons.MemoryIcon;
        _memItem.Section = "Activity & Memory";

        // One-time elevation setup item (only shown if helper service is not running)
        var setupCommand = new AnonymousCommand(() =>
        {
            TemperatureService.InstallElevationService();
        });
        _enableHelperItem = new ListItem(setupCommand)
        {
            Title = "Enable CPU Sensor Service",
            Subtitle = "Starts background helper to read CPU MSR temperatures",
            Icon = Icons.ShieldIcon,
            Section = "System"
        };

        // Initial render
        UpdateItemTexts(TemperatureService.Instance.CurrentReading);

        // Timer for real-time in-place updates (1.5s interval)
        _timer = new Timer(1500)
        {
            AutoReset = true
        };
        _timer.Elapsed += async (s, e) =>
        {
            var reading = await TemperatureService.Instance.RefreshAsync();
            UpdateItemTexts(reading);
        };
        _timer.Start();
    }

    private void UpdateItemTexts(HardwareReading reading)
    {
        // 1. Temperatures
        _cpuPkgItem.Title = $"CPU Core: {reading.FormatCpuPackage()}";
        _cpuPkgItem.Subtitle = "Package temperature";

        _cpuAvgItem.Title = $"CPU Avg: {reading.FormatCpuAverage()}";
        _cpuAvgItem.Subtitle = "Core average";

        _gpuItem.Title = $"GPU Core: {reading.FormatGpuCore()}";
        _gpuItem.Subtitle = "Graphics core temperature";

        // 2. Usages & Memory
        _cpuUsageItem.Title = $"CPU Usage: {reading.FormatCpuUsage()}";
        _cpuUsageItem.Subtitle = "Processor load";

        _gpuUsageItem.Title = $"GPU Usage: {reading.FormatGpuUsage()}";
        _gpuUsageItem.Subtitle = "Graphics 3D / core load";

        _memItem.Title = $"Memory: {reading.FormatMemoryDetailed()}";
        _memItem.Subtitle = "RAM in use";
    }

    public override IListItem[] GetItems()
    {
        var reading = TemperatureService.Instance.CurrentReading;
        if (reading.IsCpuHelperConnected)
        {
            return [_cpuPkgItem, _cpuAvgItem, _gpuItem, _cpuUsageItem, _gpuUsageItem, _memItem];
        }
        else
        {
            return [_cpuPkgItem, _cpuAvgItem, _gpuItem, _cpuUsageItem, _gpuUsageItem, _memItem, _enableHelperItem];
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
    }
}
