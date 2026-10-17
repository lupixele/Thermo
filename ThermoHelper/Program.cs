using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;
using LibreHardwareMonitor.Hardware;

namespace ThermoHelper;

public class SensorData
{
    public float CpuPackage { get; set; }
    public float CpuAverage { get; set; }
    public long TimestampTicks { get; set; }
}

public static class Program
{
    private const string PipeName = "ThermoCpuPipe";
    private static readonly string FallbackDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Thermo");
    private static readonly string FallbackFile = Path.Combine(FallbackDir, "sensors.json");
    private static readonly string LogFile = Path.Combine(FallbackDir, "helper.log");

    public static void Log(string msg)
    {
        try
        {
            File.AppendAllText(LogFile, $"[{DateTime.UtcNow:HH:mm:ss}] {msg}\n");
        }
        catch { }
    }

    public static void Main(string[] args)
    {
        Directory.CreateDirectory(FallbackDir);
        Log("ThermoHelper starting. Elevated: " + new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator));

        using var mutex = new Mutex(true, @"Global\ThermoHelperInstance", out var isNew);
        if (!isNew)
        {
            Log("Another instance already running. Exiting.");
            return;
        }

        Computer? computer = null;
        try
        {
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = false,
                IsMemoryEnabled = false,
                IsMotherboardEnabled = false,
                IsStorageEnabled = false,
                IsNetworkEnabled = false,
                IsControllerEnabled = false,
                IsBatteryEnabled = false,
                IsPsuEnabled = false
            };
            computer.Open();
            Log("Computer.Open() succeeded.");
        }
        catch (Exception ex)
        {
            Log("Computer.Open() failed: " + ex.ToString());
            computer?.Close();
            computer = null;
        }

        var sensorData = new SensorData();
        var lockObj = new object();

        // Background thread: update LHM CPU temperatures every 1.5 seconds
        var sampleThread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    if (computer != null)
                    {
                        float pkg = 0;
                        float avg = 0;
                        int coreCount = 0;
                        float coreSum = 0;

                        foreach (var hw in computer.Hardware)
                        {
                            if (hw.HardwareType == HardwareType.Cpu)
                            {
                                hw.Update();
                                foreach (var s in hw.Sensors)
                                {
                                    if (s.SensorType == SensorType.Temperature && s.Value.HasValue)
                                    {
                                        if (string.Equals(s.Name, "CPU Package", StringComparison.OrdinalIgnoreCase))
                                        {
                                            pkg = s.Value.Value;
                                        }
                                        else if (string.Equals(s.Name, "Core Average", StringComparison.OrdinalIgnoreCase))
                                        {
                                            avg = s.Value.Value;
                                        }
                                        else if (s.Name.Contains("Core #", StringComparison.OrdinalIgnoreCase))
                                        {
                                            coreSum += s.Value.Value;
                                            coreCount++;
                                        }
                                    }
                                }
                            }
                        }

                        if (avg <= 0 && coreCount > 0)
                        {
                            avg = coreSum / coreCount;
                        }

                        if (pkg > 0)
                        {
                            lock (lockObj)
                            {
                                sensorData.CpuPackage = pkg;
                                sensorData.CpuAverage = avg > 0 ? avg : pkg;
                                sensorData.TimestampTicks = DateTime.UtcNow.Ticks;
                            }

                            // Write fallback file
                            try
                            {
                                var json = JsonSerializer.Serialize(sensorData);
                                var tmpFile = FallbackFile + ".tmp";
                                File.WriteAllText(tmpFile, json);
                                File.Move(tmpFile, FallbackFile, true);
                            }
                            catch (Exception ex)
                            {
                                Log("WriteFallbackFile error: " + ex.Message);
                            }
                        }
                        else
                        {
                            Log("No CPU Package temp found in tick.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Sampling error: " + ex.ToString());
                }

                Thread.Sleep(1500);
            }
        })
        {
            IsBackground = true
        };
        sampleThread.Start();

        Log("Entering NamedPipe listening loop.");
        // Main thread: serve incoming pipe requests
        while (true)
        {
            try
            {
                var pipeSecurity = new PipeSecurity();
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    PipeAccessRights.ReadWrite,
                    AccessControlType.Allow));

                using var server = NamedPipeServerStreamAcl.Create(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    0,
                    0,
                    pipeSecurity);

                server.WaitForConnection();

                byte[] buffer;
                lock (lockObj)
                {
                    buffer = JsonSerializer.SerializeToUtf8Bytes(sensorData);
                }

                server.Write(buffer, 0, buffer.Length);
                server.Flush();
                server.Disconnect();
            }
            catch (Exception ex)
            {
                Log("Pipe server exception: " + ex.Message);
                Thread.Sleep(200);
            }
        }
    }
}
