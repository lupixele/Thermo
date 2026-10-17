using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Tasks;

namespace Thermo.Services;

public class HelperData
{
    public float CpuPackage { get; set; }
    public float CpuAverage { get; set; }
    public long TimestampTicks { get; set; }
}

public static class ElevatedSensorClient
{
    private const string PipeName = "ThermoCpuPipe";
    private static readonly string FallbackDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Thermo");
    private static readonly string FallbackFile = Path.Combine(FallbackDir, "sensors.json");

    public static async Task<HelperData?> TryReadAsync()
    {
        // 1. Try reading from Named Pipe
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(150);

            var buffer = new byte[256];
            int read = await client.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read > 0)
            {
                var data = JsonSerializer.Deserialize<HelperData>(new ReadOnlySpan<byte>(buffer, 0, read));
                if (data != null && data.CpuPackage > 0)
                {
                    return data;
                }
            }
        }
        catch
        {
        }

        // 2. Try reading from Fallback file in ProgramData
        try
        {
            if (File.Exists(FallbackFile))
            {
                var json = await File.ReadAllTextAsync(FallbackFile);
                var data = JsonSerializer.Deserialize<HelperData>(json);
                if (data != null)
                {
                    // Verify data freshness (within last 10 seconds)
                    var age = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - data.TimestampTicks);
                    if (age.TotalSeconds < 10 && data.CpuPackage > 0)
                    {
                        return data;
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }
}
