using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class MainWindowViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly NavigationViewModel _navigationViewModel;

    [ObservableProperty]
    private BaseViewModel? currentViewModel;

    [ObservableProperty]
    private NavigationViewModel navigationViewModel = new(null!, null!);

    [ObservableProperty]
    private string currentPageName = "Dashboard";

    [ObservableProperty]
    private string applicationTitle = "Donut Management System v1.0";

    [ObservableProperty]
    private bool isMenuOpen = true;

    [ObservableProperty]
    private UserContext currentUser = new();

    [ObservableProperty]
    private bool isDarkTheme = false;

    public MainWindowViewModel(
        INavigationService navigationService,
        NavigationViewModel navigationViewModel,
        ILogger<MainWindowViewModel> logger) : base(logger)
    {
        _navigationService = navigationService;
        _navigationViewModel = navigationViewModel;
        NavigationViewModel = navigationViewModel;
    }

    [RelayCommand]
    public async Task LoadApplicationAsync()
    {
        try
        {
            IsLoading = true;
            LogInfo("Initializing application...");

            CurrentUser = _navigationService.CurrentUser;
            _navigationViewModel.LoadMenuItemsCommand.Execute(null);

            await NavigateToDashboardAsync();
            
            LogInfo("Application loaded successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error loading application: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task NavigateToDashboardAsync()
    {
        if (!_navigationService.CanNavigateTo("Dashboard"))
        {
            SetError("You don't have permission to access Dashboard");
            return;
        }

        CurrentPageName = "Dashboard";
        CurrentViewModel = _navigationService.GetViewModel("Dashboard");
        
        if (CurrentViewModel is DashboardViewModel dashboardVM)
        {
            await dashboardVM.LoadDashboardCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Dashboard");
    }

    [RelayCommand]
    public async Task NavigateToRecipesAsync()
    {
        if (!_navigationService.CanNavigateTo("RecipeEditor"))
        {
            SetError("You don't have permission to access Recipes");
            return;
        }

        CurrentPageName = "Recipe Editor";
        CurrentViewModel = _navigationService.GetViewModel("RecipeEditor");

        if (CurrentViewModel is RecipeEditorViewModel recipeVM)
        {
            await recipeVM.LoadRecipesCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Recipe Editor");
    }

    [RelayCommand]
    public async Task NavigateToInventoryAsync()
    {
        if (!_navigationService.CanNavigateTo("Inventory"))
        {
            SetError("You don't have permission to access Inventory");
            return;
        }

        CurrentPageName = "Inventory";
        CurrentViewModel = _navigationService.GetViewModel("Inventory");

        if (CurrentViewModel is InventoryManagerViewModel inventoryVM)
        {
            await inventoryVM.LoadInventoryCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Inventory");
    }

    [RelayCommand]
    public async Task NavigateToBatchAsync()
    {
        if (!_navigationService.CanNavigateTo("Batch"))
        {
            SetError("You don't have permission to access Production");
            return;
        }

        CurrentPageName = "Production";
        CurrentViewModel = _navigationService.GetViewModel("Batch");

        if (CurrentViewModel is BatchManagementViewModel batchVM)
        {
            await batchVM.LoadBatchesCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Production");
    }

    [RelayCommand]
    public async Task NavigateToPricingAsync()
    {
        if (!_navigationService.CanNavigateTo("Pricing"))
        {
            SetError("You don't have permission to access Pricing");
            return;
        }

        CurrentPageName = "Pricing";
        CurrentViewModel = _navigationService.GetViewModel("Pricing");

        if (CurrentViewModel is PricingCalculatorViewModel pricingVM)
        {
            await pricingVM.LoadSKUsCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Pricing");
    }

    [RelayCommand]
    public void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;
        _navigationViewModel.ToggleMenuCommand.Execute(null);
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _navigationViewModel.ToggleThemeCommand.Execute(null);
    }
}
