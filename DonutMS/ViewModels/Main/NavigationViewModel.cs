using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class NavigationViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<MenuItemModel> menuItems = new();

    [ObservableProperty]
    private UserContext currentUser = new();

    [ObservableProperty]
    private bool isMenuOpen = true;

    [ObservableProperty]
    private bool isDarkTheme = false;

    public NavigationViewModel(
        INavigationService navigationService,
        ILogger<NavigationViewModel> logger) : base(logger)
    {
        _navigationService = navigationService;
    }

    [RelayCommand]
    public void LoadMenuItems()
    {
        try
        {
            CurrentUser = _navigationService.CurrentUser;
            var items = _navigationService.GetAvailableMenuItems();
            MenuItems = new ObservableCollection<MenuItemModel>(items);
            LogInfo("Menu items loaded successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error loading menu items: {ex.Message}");
        }
    }

    [RelayCommand]
    public void NavigateToMenu(MenuItemModel item)
    {
        try
        {
            ClearError();
            _navigationService.NavigateTo(item.ViewName);
            LogInfo($"Navigated to {item.Label}");
        }
        catch (Exception ex)
        {
            SetError($"Navigation error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    [RelayCommand]
    public void Logout()
    {
        try
        {
            _navigationService.CurrentUser = new UserContext
            {
                IsAuthenticated = false
            };
            LogInfo("User logged out");
        }
        catch (Exception ex)
        {
            SetError($"Logout error: {ex.Message}");
        }
    }
}
