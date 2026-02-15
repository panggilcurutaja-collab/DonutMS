using CommunityToolkit.Mvvm.ComponentModel;
using DonutMS.Core.MVVM;
using DonutMS.Configuration;
using DonutMS.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonutMS.Services;

public interface INavigationService
{
    void NavigateTo(string viewName);
    BaseViewModel? GetViewModel(string viewName);
    UserContext CurrentUser { get; set; }
    bool CanNavigateTo(string viewName);
    List<MenuItemModel> GetAvailableMenuItems();
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private readonly IAuthService _authService;
    private readonly Dictionary<string, Type> _viewModelMap;
    private readonly ModuleSettings _moduleSettings;

    public UserContext CurrentUser
    {
        get => _authService.CurrentUser;
        set => _authService.CurrentUser = value;
    }

    public NavigationService(
        IServiceProvider serviceProvider,
        IAuthService authService,
        IOptions<ModuleSettings> moduleOptions,
        ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _authService = authService;
        _moduleSettings = moduleOptions.Value;
        _logger = logger;

        _viewModelMap = new Dictionary<string, Type>
        {
            { "Home", typeof(DonutMS.ViewModels.HomeViewModel) },
            { "Dashboard", typeof(DonutMS.ViewModels.DashboardViewModel) },
            { "Ingredients", typeof(DonutMS.ViewModels.IngredientsViewModel) },
            { "RecipeEditor", typeof(DonutMS.ViewModels.RecipeEditorViewModel) },
            { "Substitutions", typeof(DonutMS.ViewModels.SubstitutionManagerViewModel) },
            { "Costing", typeof(DonutMS.ViewModels.CostCalculationViewModel) },
            { "SKUs", typeof(DonutMS.ViewModels.SKUMasterViewModel) },
            { "Inventory", typeof(DonutMS.ViewModels.InventoryManagerViewModel) },
            { "PurchaseOrders", typeof(DonutMS.ViewModels.PurchaseOrderViewModel) },
            { "Batch", typeof(DonutMS.ViewModels.BatchManagementViewModel) },
            { "Pricing", typeof(DonutMS.ViewModels.PricingCalculatorViewModel) },
            { "LaborOverhead", typeof(DonutMS.ViewModels.LaborOverheadViewModel) },
            { "Promotions", typeof(DonutMS.ViewModels.PromoManagerViewModel) },
            { "Reports", typeof(DonutMS.ViewModels.ReportsViewModel) },
            { "Security", typeof(DonutMS.ViewModels.SecurityViewModel) }
        };

    }

    public void NavigateTo(string viewName)
    {
        if (CanNavigateTo(viewName))
        {
            _logger.LogInformation($"Navigating to: {viewName}");
        }
        else
        {
            _logger.LogWarning($"Access denied to: {viewName}. Current role: {CurrentUser.Role}");
        }
    }

    public bool CanNavigateTo(string viewName)
    {
        // Temporarily disable role restriction for development/testing
        return _moduleSettings.IsEnabled(viewName);
    }

    public BaseViewModel? GetViewModel(string viewName)
    {
        if (!CanNavigateTo(viewName))
        {
            _logger.LogWarning($"Permission denied for ViewModel: {viewName}");
            return null;
        }

        if (_viewModelMap.TryGetValue(viewName, out var viewModelType))
        {
            try
            {
                return _serviceProvider.GetService(viewModelType) as BaseViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating ViewModel for {viewName}");
            }
        }

        return null;
    }

    public List<MenuItemModel> GetAvailableMenuItems()
    {
        var allMenuItems = new List<MenuItemModel>
        {
            new MenuItemModel
            {
                Label = "Dashboard",
                ViewName = "Dashboard",
                Icon = "Home",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager | UserRole.Operator | UserRole.Kasir,
                Order = 1
            },
            new MenuItemModel
            {
                Label = "Ingredients",
                ViewName = "Ingredients",
                Icon = "Palette",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 2
            },
            new MenuItemModel
            {
                Label = "Recipes",
                ViewName = "RecipeEditor",
                Icon = "Palette",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 3
            },
            new MenuItemModel
            {
                Label = "Substitutions",
                ViewName = "Substitutions",
                Icon = "SwapHorizontal",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 4
            },
            new MenuItemModel
            {
                Label = "Costing",
                ViewName = "Costing",
                Icon = "Calculator",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 5
            },
            new MenuItemModel
            {
                Label = "SKU Master",
                ViewName = "SKUs",
                Icon = "Tag",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 6
            },
            new MenuItemModel
            {
                Label = "Inventory",
                ViewName = "Inventory",
                Icon = "Package",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 7
            },
            new MenuItemModel
            {
                Label = "Purchase Orders",
                ViewName = "PurchaseOrders",
                Icon = "Truck",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 8
            },
            new MenuItemModel
            {
                Label = "Production",
                ViewName = "Batch",
                Icon = "Wrench",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager | UserRole.Operator,
                Order = 9
            },
            new MenuItemModel
            {
                Label = "Pricing",
                ViewName = "Pricing",
                Icon = "CurrencyUsd",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager | UserRole.Kasir,
                Order = 10
            },
            new MenuItemModel
            {
                Label = "Labor & Overhead",
                ViewName = "LaborOverhead",
                Icon = "AccountGroup",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 11
            },
            new MenuItemModel
            {
                Label = "Promo Manager",
                ViewName = "Promotions",
                Icon = "Percent",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 12
            },
            new MenuItemModel
            {
                Label = "Reports",
                ViewName = "Reports",
                Icon = "ChartBar",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 13
            },
            new MenuItemModel
            {
                Label = "Security & Audit",
                ViewName = "Security",
                Icon = "ShieldAccount",
                RequiredRoles = UserRole.Admin,
                Order = 14
            }
        };

        // Temporarily disable role restriction for development/testing
        return allMenuItems
            .Where(m => _moduleSettings.IsEnabled(m.ViewName))
            .OrderBy(m => m.Order)
            .ToList();
    }
}
