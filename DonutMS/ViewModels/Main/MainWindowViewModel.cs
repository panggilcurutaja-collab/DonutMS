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
    private readonly IWindowService _windowService;

    [ObservableProperty]
    private BaseViewModel? currentViewModel;

    [ObservableProperty]
    private NavigationViewModel navigationViewModel = null!;

    [ObservableProperty]
    private string currentPageName = "Dashboard";

    [ObservableProperty]
    private string applicationTitle = "Donut Management System v1.0";

    [ObservableProperty]
    private bool isMenuOpen = true;

    [ObservableProperty]
    private UserContext currentUser = new();

    public MainWindowViewModel(
        INavigationService navigationService,
        NavigationViewModel navigationViewModel,
        IWindowService windowService,
        ILogger<MainWindowViewModel> logger) : base(logger)
    {
        _navigationService = navigationService;
        _navigationViewModel = navigationViewModel;
        _windowService = windowService;
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

            CurrentPageName = "Home";
            CurrentViewModel = _navigationService.GetViewModel("Home");
            if (CurrentViewModel == null)
            {
                CurrentPageName = "Dashboard";
                CurrentViewModel = _navigationService.GetViewModel("Dashboard");
            }

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
    public async Task NavigateToMenuAsync(MenuItemModel item)
    {
        if (item == null)
            return;

        ClearError();

        switch (item.ViewName)
        {
            case "Dashboard":
                await NavigateToDashboardAsync();
                break;
            case "Ingredients":
                await NavigateToIngredientsAsync();
                break;
            case "RecipeEditor":
                await NavigateToRecipesAsync();
                break;
            case "Substitutions":
                await NavigateToSubstitutionsAsync();
                break;
            case "Costing":
                await NavigateToCostingAsync();
                break;
            case "SKUs":
                await NavigateToSkusAsync();
                break;
            case "Inventory":
                await NavigateToInventoryAsync();
                break;
            case "PurchaseOrders":
                await NavigateToPurchaseOrdersAsync();
                break;
            case "Batch":
                await NavigateToBatchAsync();
                break;
            case "Pricing":
                await NavigateToPricingAsync();
                break;
            case "LaborOverhead":
                await NavigateToLaborOverheadAsync();
                break;
            case "Promotions":
                await NavigateToPromotionsAsync();
                break;
            case "Reports":
                await NavigateToReportsAsync();
                break;
            case "Security":
                await NavigateToSecurityAsync();
                break;
            default:
                SetError($"Unknown menu: {item.Label}");
                break;
        }
    }

    [RelayCommand]
    public async Task NavigateToIngredientsAsync()
    {
        if (!_navigationService.CanNavigateTo("Ingredients"))
        {
            SetError("You don't have permission to access Ingredients");
            return;
        }

        CurrentPageName = "Ingredients";
        CurrentViewModel = _navigationService.GetViewModel("Ingredients");

        if (CurrentViewModel is IngredientsViewModel ingredientsVM)
        {
            await ingredientsVM.LoadIngredientsCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Ingredients");
    }

    [RelayCommand]
    public async Task NavigateToSubstitutionsAsync()
    {
        if (!_navigationService.CanNavigateTo("Substitutions"))
        {
            SetError("You don't have permission to access Substitutions");
            return;
        }

        CurrentPageName = "Substitutions";
        CurrentViewModel = _navigationService.GetViewModel("Substitutions");

        if (CurrentViewModel is SubstitutionManagerViewModel substitutionVM)
        {
            await substitutionVM.LoadCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Substitutions");
    }

    [RelayCommand]
    public async Task NavigateToCostingAsync()
    {
        if (!_navigationService.CanNavigateTo("Costing"))
        {
            SetError("You don't have permission to access Costing");
            return;
        }

        CurrentPageName = "Costing";
        CurrentViewModel = _navigationService.GetViewModel("Costing");

        if (CurrentViewModel is CostCalculationViewModel costingVM)
        {
            await costingVM.LoadCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Costing");
    }

    [RelayCommand]
    public async Task NavigateToSkusAsync()
    {
        if (!_navigationService.CanNavigateTo("SKUs"))
        {
            SetError("You don't have permission to access SKU Master");
            return;
        }

        CurrentPageName = "SKU Master";
        CurrentViewModel = _navigationService.GetViewModel("SKUs");

        if (CurrentViewModel is SKUMasterViewModel skuVM)
        {
            await skuVM.LoadSkusCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to SKU Master");
    }

    [RelayCommand]
    public async Task NavigateToPurchaseOrdersAsync()
    {
        if (!_navigationService.CanNavigateTo("PurchaseOrders"))
        {
            SetError("You don't have permission to access Purchase Orders");
            return;
        }

        CurrentPageName = "Purchase Orders";
        CurrentViewModel = _navigationService.GetViewModel("PurchaseOrders");

        if (CurrentViewModel is PurchaseOrderViewModel poVM)
        {
            await poVM.LoadPurchaseOrdersCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Purchase Orders");
    }

    [RelayCommand]
    public async Task NavigateToLaborOverheadAsync()
    {
        if (!_navigationService.CanNavigateTo("LaborOverhead"))
        {
            SetError("You don't have permission to access Labor & Overhead");
            return;
        }

        CurrentPageName = "Labor & Overhead";
        CurrentViewModel = _navigationService.GetViewModel("LaborOverhead");

        if (CurrentViewModel is LaborOverheadViewModel laborVM)
        {
            await laborVM.LoadCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Labor & Overhead");
    }

    [RelayCommand]
    public async Task NavigateToPromotionsAsync()
    {
        if (!_navigationService.CanNavigateTo("Promotions"))
        {
            SetError("You don't have permission to access Promo Manager");
            return;
        }

        CurrentPageName = "Promo Manager";
        CurrentViewModel = _navigationService.GetViewModel("Promotions");

        if (CurrentViewModel is PromoManagerViewModel promoVM)
        {
            await promoVM.LoadCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Promo Manager");
    }

    [RelayCommand]
    public async Task NavigateToReportsAsync()
    {
        if (!_navigationService.CanNavigateTo("Reports"))
        {
            SetError("You don't have permission to access Reports");
            return;
        }

        CurrentPageName = "Reports";
        CurrentViewModel = _navigationService.GetViewModel("Reports");

        if (CurrentViewModel is ReportsViewModel reportsVM)
        {
            await reportsVM.LoadReportsCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Reports");
    }

    [RelayCommand]
    public async Task NavigateToSecurityAsync()
    {
        if (!_navigationService.CanNavigateTo("Security"))
        {
            SetError("You don't have permission to access Security & Audit");
            return;
        }

        CurrentPageName = "Security & Audit";
        CurrentViewModel = _navigationService.GetViewModel("Security");

        if (CurrentViewModel is SecurityViewModel securityVM)
        {
            await securityVM.LoadCommand.ExecuteAsync(null);
        }

        LogInfo("Navigated to Security & Audit");
    }

    [RelayCommand]
    public void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;
        _navigationViewModel.ToggleMenuCommand.Execute(null);
    }

    [RelayCommand]
    public void OpenRegister()
    {
        try
        {
            ClearError();
            _windowService.ShowRegisterWindow();
        }
        catch (Exception ex)
        {
            SetError($"Unable to open register window: {ex.Message}");
        }
    }
}
