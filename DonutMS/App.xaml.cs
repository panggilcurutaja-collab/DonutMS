using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using DonutMS.Configuration;
using DonutMS.ViewModels;

namespace DonutMS;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = LoggingConfiguration.ConfigureLogging();

        try
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    var basePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? Directory.GetCurrentDirectory();
                    config.SetBasePath(basePath)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddSerilog();
                    });

                    var connectionString = context.Configuration.GetConnectionString("Default") ?? "Data Source=donutms.db";
                    services.AddApplicationServices(connectionString);

                    // Register NavigationViewModel for MainWindowViewModel
                    services.AddScoped<NavigationViewModel>();
                    services.AddScoped<MainWindow>();
                })
                .Build();

            ServiceProvider = AppHost.Services;

            // Create and show MainWindow with dependency injection
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application initialization failed");
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
        AppHost?.Dispose();
        base.OnExit(e);
    }
}
