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

            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "DonutMS")
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 104857600, // 100 MB
                    retainedFileCountLimit: 10)
                .CreateLogger();

            return logger;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CRITICAL: Error configuring logging: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}



