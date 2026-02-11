using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DonutMS.Services;

/// <summary>
/// Service untuk resolve Views dari ViewModels secara otomatis
/// Menggunakan konvensi naming: MyViewModel -> MyView
/// </summary>
public interface IViewLocator
{
    /// <summary>
    /// Locate dan create View instance untuk given ViewModel type
    /// </summary>
    UIElement? GetViewForViewModel(Type viewModelType);
    
    /// <summary>
    /// Register custom view type untuk ViewModel type
    /// </summary>
    void RegisterViewMapping(Type viewModelType, Type viewType);
}

public class ViewLocator : IViewLocator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _viewMappings;

    public ViewLocator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _viewMappings = new Dictionary<Type, Type>();
        
        // Register default mappings berdasarkan naming convention
        RegisterDefaultMappings();
    }

    /// <summary>
    /// Register default view mappings using naming convention
    /// </summary>
    private void RegisterDefaultMappings()
    {
        // Manual mappings untuk views yang ada
        RegisterViewMapping(
            typeof(DonutMS.ViewModels.DashboardViewModel),
            typeof(DonutMS.Views.Main.DashboardView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.HomeViewModel),
            typeof(DonutMS.Views.Main.HomeView));
        
        RegisterViewMapping(
            typeof(DonutMS.ViewModels.IngredientsViewModel),
            typeof(DonutMS.Views.Ingredients.IngredientListView));
        
        RegisterViewMapping(
            typeof(DonutMS.ViewModels.RecipeEditorViewModel),
            typeof(DonutMS.Views.Recipes.RecipeListView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.SubstitutionManagerViewModel),
            typeof(DonutMS.Views.Recipes.SubstitutionManagerView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.CostCalculationViewModel),
            typeof(DonutMS.Views.Costing.CostCalculationView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.InventoryManagerViewModel),
            typeof(DonutMS.Views.Inventory.InventoryView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.SKUMasterViewModel),
            typeof(DonutMS.Views.SKUs.SKUMasterView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.PurchaseOrderViewModel),
            typeof(DonutMS.Views.PurchaseOrders.PurchaseOrdersView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.BatchManagementViewModel),
            typeof(DonutMS.Views.Production.BatchManagementView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.LaborOverheadViewModel),
            typeof(DonutMS.Views.Labor.LaborOverheadViewV2));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.PricingCalculatorViewModel),
            typeof(DonutMS.Views.Pricing.PricingCalculatorView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.PromoManagerViewModel),
            typeof(DonutMS.Views.Promotions.PromoManagerViewV2));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.ReportsViewModel),
            typeof(DonutMS.Views.Reports.ReportsView));

        RegisterViewMapping(
            typeof(DonutMS.ViewModels.SecurityViewModel),
            typeof(DonutMS.Views.Admin.SecurityView));
    }

    public UIElement? GetViewForViewModel(Type viewModelType)
    {
        if (viewModelType == null)
            throw new ArgumentNullException(nameof(viewModelType));

        // Check if registered mapping exists
        if (_viewMappings.TryGetValue(viewModelType, out var viewType))
        {
            try
            {
                var view = _serviceProvider.GetService(viewType) as UIElement;
                return view;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating view for {viewModelType.Name}: {ex.Message}");
                return null;
            }
        }

        // Try auto-resolution using naming convention
        var viewName = viewModelType.Name.Replace("ViewModel", "View");
        var viewTypeName = viewModelType.Namespace?.Replace("ViewModels", "Views") + "." + viewName;

        if (!string.IsNullOrEmpty(viewTypeName))
        {
            try
            {
                var type = Type.GetType(viewTypeName, false);
                if (type != null)
                {
                    var view = _serviceProvider.GetService(type) as UIElement;
                    return view;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-resolving view {viewTypeName}: {ex.Message}");
            }
        }

        return null;
    }

    public void RegisterViewMapping(Type viewModelType, Type viewType)
    {
        if (viewModelType == null)
            throw new ArgumentNullException(nameof(viewModelType));
        if (viewType == null)
            throw new ArgumentNullException(nameof(viewType));

        if (!typeof(System.Windows.FrameworkElement).IsAssignableFrom(viewType))
            throw new ArgumentException($"{viewType.Name} must derive from FrameworkElement", nameof(viewType));

        _viewMappings[viewModelType] = viewType;
    }
}
