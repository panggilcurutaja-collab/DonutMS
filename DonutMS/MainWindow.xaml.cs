using MahApps.Metro.Controls;
using DonutMS.ViewModels;

namespace DonutMS;

public partial class MainWindow : MetroWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.LoadApplicationCommand.ExecuteAsync(null);
        }
    }
}
