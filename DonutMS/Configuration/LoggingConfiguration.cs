using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DonutMS.Configuration;

public static class LoggingConfiguration
{
    public static Logger ConfigureLogging(string logsFolder = "Logs")
    {
        try
        {
            if (!Directory.Exists(logsFolder))
                Directory.CreateDirectory(logsFolder);

            var logPath = Path.Combine(logsFolder, "donutms-.log");

            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error configuring logging: {ex.Message}");
            throw;
        }
    }
}
