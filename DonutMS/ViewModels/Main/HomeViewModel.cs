using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;

namespace DonutMS.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    [ObservableProperty]
    private string welcomeTitle = "Welcome to Donut Management System";

    [ObservableProperty]
    private string welcomeSubtitle = "Pilih menu di kiri untuk mulai bekerja.";

    public HomeViewModel(ILogger<HomeViewModel> logger) : base(logger)
    {
    }

    [RelayCommand]
    public void Refresh()
    {
        LogInfo("Home refreshed");
    }
}
