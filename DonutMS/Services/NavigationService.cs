using CommunityToolkit.Mvvm.ComponentModel;
using DonutMS.Core.MVVM;
using DonutMS.Models;
using Microsoft.Extensions.Logging;

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
    private readonly Dictionary<string, Type> _viewModelMap;
    private UserContext _currentUser = new();

    public UserContext CurrentUser
    {
        get => _currentUser;
        set => _currentUser = value;
    }

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _viewModelMap = new Dictionary<string, Type>
        {
            { "Dashboard", typeof(DonutMS.ViewModels.DashboardViewModel) },
            { "Ingredients", typeof(DonutMS.ViewModels.IngredientsViewModel) },
            { "RecipeEditor", typeof(DonutMS.ViewModels.RecipeEditorViewModel) },
            { "Inventory", typeof(DonutMS.ViewModels.InventoryManagerViewModel) },
            { "Batch", typeof(DonutMS.ViewModels.BatchManagementViewModel) },
            { "Pricing", typeof(DonutMS.ViewModels.PricingCalculatorViewModel) }
        };

        InitializeDefaultUser();
    }

    private void InitializeDefaultUser()
    {
        _currentUser = new UserContext
        {
            UserId = 1,
            Username = "admin",
            FullName = "Administrator",
            Role = UserRole.Admin,
            IsAuthenticated = true,
            LoginTime = DateTime.Now
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
            _logger.LogWarning($"Access denied to: {viewName}. Current role: {_currentUser.Role}");
        }
    }

    public bool CanNavigateTo(string viewName)
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        // Map views to required roles
        var roleRequirements = new Dictionary<string, UserRole>
        {
            { "Dashboard", UserRole.Admin | UserRole.ProduksionManager | UserRole.Operator | UserRole.Kasir },
            { "Ingredients", UserRole.Admin | UserRole.ProduksionManager },
            { "RecipeEditor", UserRole.Admin | UserRole.ProduksionManager },
            { "Inventory", UserRole.Admin | UserRole.ProduksionManager },
            { "Batch", UserRole.Admin | UserRole.ProduksionManager | UserRole.Operator },
            { "Pricing", UserRole.Admin | UserRole.ProduksionManager | UserRole.Kasir }
        };

        if (!roleRequirements.TryGetValue(viewName, out var requiredRoles))
            return false;

        return (_currentUser.Role & requiredRoles) != 0;
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
                Label = "Inventory",
                ViewName = "Inventory",
                Icon = "Package",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager,
                Order = 4
            },
            new MenuItemModel
            {
                Label = "Production",
                ViewName = "Batch",
                Icon = "Wrench",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager | UserRole.Operator,
                Order = 5
            },
            new MenuItemModel
            {
                Label = "Pricing",
                ViewName = "Pricing",
                Icon = "CurrencyUsd",
                RequiredRoles = UserRole.Admin | UserRole.ProduksionManager | UserRole.Kasir,
                Order = 6
            }
        };

        return allMenuItems
            .Where(m => m.IsVisibleForRole(_currentUser.Role))
            .OrderBy(m => m.Order)
            .ToList();
    }
}
