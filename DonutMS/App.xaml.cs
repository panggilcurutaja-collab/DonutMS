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

        // Stage 1: Configure logging FIRST before anything else
        try
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("🚀 DonutMS Application Starting...");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Stage 1: Configuring Serilog logger...");

            Log.Logger = LoggingConfiguration.ConfigureLogging();
            Log.Information("✅ Serilog logger configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Stage 1 FAILED: Error configuring logging");
            Console.WriteLine($"   Exception: {ex.GetType().Name}");
            Console.WriteLine($"   Message: {ex.Message}");
            Console.WriteLine($"   StackTrace: {ex.StackTrace}");
            MessageBox.Show($"Critical Error: Failed to configure logging\n\n{ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        try
        {
            // Stage 2: Configure application settings
            Log.Information("═══════════════════════════════════════════════════════════");
            Log.Information("[Stage 2] Configuring application settings...");
            Log.Information($"  Base Path: {AppDomain.CurrentDomain.BaseDirectory}");
            Log.Information($"  Environment: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}");

            // Stage 3: Build Host
            Log.Information("[Stage 3] Creating IHost with dependency injection...");

            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    Log.Debug("  - Configuring app configuration...");
                    var basePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? Directory.GetCurrentDirectory();
                    config.SetBasePath(basePath);

                    var settingsFile = "appsettings.json";
                    Log.Debug($"  - Loading {settingsFile}...");
                    config.AddJsonFile(settingsFile, optional: false, reloadOnChange: true);

                    var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
                    var envSettingsFile = $"appsettings.{envName}.json";
                    Log.Debug($"  - Loading {envSettingsFile} (if exists)...");
                    config.AddJsonFile(envSettingsFile, optional: true);

                    Log.Information("  ✅ App configuration loaded");
                })
                .ConfigureServices((context, services) =>
                {
                    try
                    {
                        Log.Information("[Stage 4] Registering services with DI container...");

                        // Setup logging
                        Log.Debug("  - Adding Serilog to logging...");
                        services.AddLogging(logging =>
                        {
                            logging.ClearProviders();
                            logging.AddSerilog();
                        });

                        // Database
                        Log.Debug("  - Configuring database connection...");
                        var connectionString = context.Configuration.GetConnectionString("Default") ?? "Data Source=donutms.db";
                        Log.Information($"  - Connection String: {connectionString}");

                        // Application services
                        Log.Debug("  - Adding application services...");
                        services.AddApplicationServices(connectionString);
                        Log.Debug("    ✅ Application services registered");

                        // Windows / Shell
                        Log.Debug("  - Registering windows...");
                        services.AddScoped<MainWindow>();
                        Log.Debug("    ✅ Windows registered");

                        Log.Information("  ✅ All services registered successfully");
                    }
                    catch (Exception serviceEx)
                    {
                        Log.Fatal(serviceEx, "❌ Error during service registration");
                        throw;
                    }
                })
                .Build();

            Log.Information("✅ IHost created successfully");

            // Stage 5: Get service provider
            Log.Information("[Stage 5] Creating service provider...");
            ServiceProvider = AppHost.Services;
            Log.Information("✅ Service provider created");

            // Stage 6: Initialize database
            Log.Information("[Stage 6] Initializing database...");
            try
            {
                var dbInitializer = ServiceProvider.GetRequiredService<DonutMS.Data.DbContext.DatabaseInitializer>();
                Log.Debug("  - Running database initialization...");
                dbInitializer.InitializeAsync().GetAwaiter().GetResult();
                Log.Information("✅ Database initialized");
            }
            catch (Exception dbEx)
            {
                Log.Warning(dbEx, "⚠️ Database initialization warning (non-critical)");
                // Non-fatal, continue anyway
            }

            // Stage 7: Create main window
            Log.Information("[Stage 7] Creating main window...");
            MainWindow mainWindow = null;
            try
            {
                mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                Log.Debug("  - MainWindow instance created");
                Log.Debug("  - Setting window DataContext...");
                // DataContext should be set via DI, but verify
                Log.Information("✅ MainWindow created successfully");
            }
            catch (Exception windowEx)
            {
                Log.Fatal(windowEx, "❌ Error creating MainWindow");
                throw;
            }

            // Stage 8: Show main window
            Log.Information("[Stage 8] Displaying main window...");
            try
            {
                mainWindow?.Show();
                Log.Information("✅ MainWindow displayed");
            }
            catch (Exception showEx)
            {
                Log.Fatal(showEx, "❌ Error showing MainWindow");
                throw;
            }

            // Success!
            Log.Information("═══════════════════════════════════════════════════════════");
            Log.Information("🎉 Application started successfully!");
            Log.Information("═══════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "═══════════════════════════════════════════════════════════");
            Log.Fatal("❌ APPLICATION STARTUP FAILED");
            Log.Fatal("═══════════════════════════════════════════════════════════");
            Log.Fatal($"Exception Type: {ex.GetType().FullName}");
            Log.Fatal($"Message: {ex.Message}");
            Log.Fatal($"Inner Exception: {ex.InnerException?.Message}");

            var errorMessage = $@"
❌ DONUTMS STARTUP ERROR

Exception: {ex.GetType().Name}
Message: {ex.Message}

Inner Exception: {ex.InnerException?.Message}

StackTrace:
{ex.StackTrace}

Please check the logs in the 'Logs' folder for more details.
";
            MessageBox.Show(errorMessage, "DonutMS Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            Log.CloseAndFlush();
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("═══════════════════════════════════════════════════════════");
        Log.Information("👋 Application shutting down...");
        Log.Information("═══════════════════════════════════════════════════════════");
        Log.CloseAndFlush();
        AppHost?.Dispose();
        base.OnExit(e);
    }
}
