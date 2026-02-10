using System;
using MahApps.Metro.Controls;
using Serilog;
using DonutMS.ViewModels;
using DonutMS.Services;

namespace DonutMS;

public partial class MainWindow : MetroWindow
{
    private readonly IViewLocator _viewLocator;

    public MainWindow(MainWindowViewModel viewModel, IViewLocator viewLocator)
    {
        try
        {
            Log.Information("[MainWindow.ctor] Initializing MainWindow...");
            InitializeComponent();
            Log.Debug("  ✅ InitializeComponent completed");

            _viewLocator = viewLocator ?? throw new ArgumentNullException(nameof(viewLocator));

            Log.Debug("  - Setting DataContext to MainWindowViewModel...");
            DataContext = viewModel;
            
            // Wire up view model changes to update ContentControl
            if (viewModel != null)
            {
                viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainWindowViewModel.CurrentViewModel))
                    {
                        UpdateContentView(viewModel.CurrentViewModel);
                    }
                };
            }

            Log.Information("[MainWindow.ctor] ✅ MainWindow constructor completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[MainWindow.ctor] ❌ Error in MainWindow constructor");
            throw;
        }
    }

    private void UpdateContentView(object? viewModel)
    {
        if (viewModel == null)
        {
            ContentArea.Content = null;
            return;
        }

        try
        {
            Log.Debug($"[UpdateContentView] Resolving view for {viewModel.GetType().Name}...");
            var view = _viewLocator.GetViewForViewModel(viewModel.GetType());
            
            if (view != null)
            {
                if (view is System.Windows.FrameworkElement frameworkElement)
                {
                    frameworkElement.DataContext = viewModel;
                }
                ContentArea.Content = view;
                Log.Information($"[UpdateContentView] ✅ View loaded for {viewModel.GetType().Name}");
            }
            else
            {
                Log.Warning($"[UpdateContentView] ⚠️ No view found for {viewModel.GetType().Name}");
                ContentArea.Content = new System.Windows.Controls.TextBlock
                {
                    Text = $"No view configured for {viewModel.GetType().Name}",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Red
                };
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[UpdateContentView] Error updating content view");
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
