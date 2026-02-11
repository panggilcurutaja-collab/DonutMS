using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using DonutMS.Configuration;
using DonutMS.Services;
using DonutMS.ViewModels;

namespace DonutMS;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

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

                        // App settings
                        services.Configure<ModuleSettings>(context.Configuration.GetSection("Modules"));

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

            // Stage 6: Initialize database (scoped)
            Log.Information("[Stage 6] Initializing database...");
            try
            {
                using var initScope = ServiceProvider.CreateScope();
                var dbInitializer = initScope.ServiceProvider.GetRequiredService<DonutMS.Data.DbContext.DatabaseInitializer>();
                Log.Debug("  - Running database initialization...");
                dbInitializer.InitializeAsync().GetAwaiter().GetResult();
                Log.Information("✅ Database initialized");
            }
            catch (Exception dbEx)
            {
                Log.Warning(dbEx, "⚠️ Database initialization warning (non-critical)");
                // Non-fatal, continue anyway
            }

            // Stage 6.5: Auto backup database (daily)
            Log.Information("[Stage 6.5] Checking daily database backup...");
            try
            {
                using var backupScope = ServiceProvider.CreateScope();
                var backupService = backupScope.ServiceProvider.GetRequiredService<IBackupService>();
                var backupPath = backupService.EnsureDailyBackupAsync().GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(backupPath))
                {
                    Log.Information("✅ Auto backup created: {Path}", backupPath);
                }
            }
            catch (Exception backupEx)
            {
                Log.Warning(backupEx, "⚠️ Auto backup skipped due to error (non-critical)");
            }

            // Stage 6.6: Show login window
            Log.Information("[Stage 6.6] Displaying login window...");
            try
            {
                var loginWindow = ServiceProvider.GetRequiredService<DonutMS.Views.Auth.LoginWindow>();
                var loginResult = loginWindow.ShowDialog();
                if (loginResult != true)
                {
                    Log.Warning("Login cancelled or failed. Shutting down application.");
                    Shutdown(0);
                    return;
                }
            }
            catch (Exception loginEx)
            {
                Log.Fatal(loginEx, "❌ Error displaying login window");
                throw;
            }

            // Stage 6.7: Load UI resource dictionaries after login
            Log.Information("[Stage 6.7] Loading UI theme resources...");
            try
            {
                LoadUiResources();
                Log.Information("✅ UI resources loaded");
            }
            catch (Exception themeEx)
            {
                Log.Fatal(themeEx, "❌ Error loading UI resources");
                throw;
            }

            // Stage 7: Create main window
            Log.Information("[Stage 7] Creating main window...");
            MainWindow? mainWindow = null;
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
                Application.Current.MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
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

    private static void LoadUiResources()
    {
        var app = Application.Current;
        if (app == null)
            return;

        var uri = new Uri("pack://application:,,,/DonutMS;component/Resources/Styles/AppTheme.xaml", UriKind.Absolute);
        var alreadyLoaded = app.Resources.MergedDictionaries.Any(d => d.Source != null && d.Source == uri);
        if (alreadyLoaded)
            return;

        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
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
