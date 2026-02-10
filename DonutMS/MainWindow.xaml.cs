using System;
using MahApps.Metro.Controls;
using Serilog;
using DonutMS.ViewModels;

namespace DonutMS;

public partial class MainWindow : MetroWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        try
        {
            Log.Information("[MainWindow.ctor] Initializing MainWindow...");
            InitializeComponent();
            Log.Debug("  ✅ InitializeComponent completed");

            Log.Debug("  - Setting DataContext to MainWindowViewModel...");
            DataContext = viewModel;
            Log.Information("[MainWindow.ctor] ✅ MainWindow constructor completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[MainWindow.ctor] ❌ Error in MainWindow constructor");
            throw;
        }
    }

    private void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            Log.Information("[MainWindow.Loaded] Window loaded event triggered");
            
            if (DataContext is MainWindowViewModel viewModel)
            {
                Log.Debug("  - DataContext is MainWindowViewModel, executing LoadApplicationCommand...");
                viewModel.LoadApplicationCommand.ExecuteAsync(null);
                Log.Information("[MainWindow.Loaded] ✅ LoadApplicationCommand executed");
            }
            else
            {
                Log.Warning("[MainWindow.Loaded] ⚠️ DataContext is not MainWindowViewModel");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[MainWindow.Loaded] ❌ Error in window loaded event");
            throw;
        }
    }
}
