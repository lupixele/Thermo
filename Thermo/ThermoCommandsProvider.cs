// Copyright (c) Microsoft Corporation and Thermo contributors. MIT license.

using System;
using System.Timers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Thermo.Pages;
using Thermo.Services;

namespace Thermo;

public partial class ThermoCommandsProvider : CommandProvider
{
    private const string BaseId = "thermo.hardware";

    private readonly CommandItem _mainCommand;

    // Single standalone list items for the dock buttons
    private readonly ListItem _cpuCoreItem;
    private readonly ListItem _cpuAvgItem;
    private readonly ListItem _gpuCoreItem;
    private readonly ListItem _cpuUsageItem;
    private readonly ListItem _gpuUsageItem;
    private readonly ListItem _memoryItem;

    // Separate wrapped dock bands - each appears as a single independent item in the Dock edit list
    private readonly WrappedDockItem _cpuCoreBand;
    private readonly WrappedDockItem _cpuAvgBand;
    private readonly WrappedDockItem _gpuCoreBand;
    private readonly WrappedDockItem _cpuUsageBand;
    private readonly WrappedDockItem _gpuUsageBand;
    private readonly WrappedDockItem _memoryBand;

    private readonly ICommandItem[] _commands;
    private readonly ICommandItem[] _dockBands;
    private readonly Timer _summaryTimer;

    public ThermoCommandsProvider()
    {
        DisplayName = "Thermo";
        Id = BaseId;
        Icon = Icons.ThermoIcon;

        var page = new TemperaturePage();

        // 1. Main command for Command Palette search list
        _mainCommand = new CommandItem(page)
        {
            Title = "Thermo",
            Subtitle = "Loading hardware metrics...",
            Icon = Icons.ThermoIcon
        };

        // 2. Individual standalone dock items (single button each)
        _cpuCoreItem = new ListItem(page)
        {
            Title = "CPU Core: --",
            Icon = Icons.CpuIcon
        };

        _cpuAvgItem = new ListItem(page)
        {
            Title = "CPU Avg: --",
            Icon = Icons.CpuIcon
        };

        _gpuCoreItem = new ListItem(page)
        {
            Title = "GPU Core: --",
            Icon = Icons.GpuIcon
        };

        _cpuUsageItem = new ListItem(page)
        {
            Title = "CPU: --",
            Icon = Icons.ActivityIcon
        };

        _gpuUsageItem = new ListItem(page)
        {
            Title = "GPU: --",
            Icon = Icons.ActivityIcon
        };

        _memoryItem = new ListItem(page)
        {
            Title = "RAM: --",
            Icon = Icons.MemoryIcon
        };

        // 3. Wrap each in its own distinct WrappedDockItem with its own unique band ID
        _cpuCoreBand = new WrappedDockItem([_cpuCoreItem], $"{BaseId}.cpucore", "CPU Core Temp")
        {
            Icon = Icons.CpuIcon
        };

        _cpuAvgBand = new WrappedDockItem([_cpuAvgItem], $"{BaseId}.cpuavg", "CPU Avg Temp")
        {
            Icon = Icons.CpuIcon
        };

        _gpuCoreBand = new WrappedDockItem([_gpuCoreItem], $"{BaseId}.gpucore", "GPU Core Temp")
        {
            Icon = Icons.GpuIcon
        };

        _cpuUsageBand = new WrappedDockItem([_cpuUsageItem], $"{BaseId}.cpuusage", "CPU Usage")
        {
            Icon = Icons.ActivityIcon
        };

        _gpuUsageBand = new WrappedDockItem([_gpuUsageItem], $"{BaseId}.gpuusage", "GPU Usage")
        {
            Icon = Icons.ActivityIcon
        };

        _memoryBand = new WrappedDockItem([_memoryItem], $"{BaseId}.memory", "Memory (RAM)")
        {
            Icon = Icons.MemoryIcon
        };

        _commands = [_mainCommand];
        _dockBands = [_cpuCoreBand, _cpuAvgBand, _gpuCoreBand, _cpuUsageBand, _gpuUsageBand, _memoryBand];

        UpdateSubtitles();

        // Update all dock bands periodically
        _summaryTimer = new Timer(1500)
        {
            AutoReset = true
        };
        _summaryTimer.Elapsed += (s, e) =>
        {
            UpdateSubtitles();
        };
        _summaryTimer.Start();
    }

    private void UpdateSubtitles()
    {
        var reading = TemperatureService.Instance.CurrentReading;

        // Update main palette command subtitle
        _mainCommand.Subtitle = reading.GetSummaryString();

        // Update individual dock buttons
        _cpuCoreItem.Title = $"CPU Core: {reading.FormatCpuPackage()}";
        _cpuAvgItem.Title = $"CPU Avg: {reading.FormatCpuAverage()}";
        _gpuCoreItem.Title = $"GPU Core: {reading.FormatGpuCore()}";

        _cpuUsageItem.Title = $"CPU: {reading.FormatCpuUsage()}";
        _gpuUsageItem.Title = $"GPU: {reading.FormatGpuUsage()}";
        _memoryItem.Title = $"RAM: {reading.FormatMemoryUsage()}";
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

    public override ICommandItem[]? GetDockBands()
    {
        return _dockBands;
    }
}
