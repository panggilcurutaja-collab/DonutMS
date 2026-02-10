using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IReportingService _reportingService;
    private readonly ICostCalculationService _costCalculationService;
    private readonly IInventoryService _inventoryService;
    private readonly IPricingService _pricingService;

    [ObservableProperty]
    private decimal averageHPP;

    [ObservableProperty]
    private decimal totalMaterialCost;

    [ObservableProperty]
    private decimal costPerUnit;

    [ObservableProperty]
    private decimal grossMarginPercent;

    [ObservableProperty]
    private int lowStockCount;

    [ObservableProperty]
    private int activeBatchCount;

    [ObservableProperty]
    private decimal monthlyRevenue;

    [ObservableProperty]
    private decimal totalProfit;

    [ObservableProperty]
    private ObservableCollection<SKUDto> topSKUs = new();

    [ObservableProperty]
    private ObservableCollection<string> recentActivities = new();

    public DashboardViewModel(
        IReportingService reportingService,
        ICostCalculationService costCalculationService,
        IInventoryService inventoryService,
        IPricingService pricingService,
        ILogger<DashboardViewModel> logger) : base(logger)
    {
        _reportingService = reportingService;
        _costCalculationService = costCalculationService;
        _inventoryService = inventoryService;
        _pricingService = pricingService;
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var fromDate = DateTime.UtcNow.AddMonths(-1);
            var toDate = DateTime.UtcNow;

            AverageHPP = await _reportingService.GetAverageHPPAsync(fromDate, toDate);
            TotalMaterialCost = await _reportingService.GetTotalMaterialCostAsync(fromDate, toDate);
            CostPerUnit = AverageHPP;

            var lowStocks = await _inventoryService.GetLowStockItemsAsync();
            LowStockCount = lowStocks.Count();

            GrossMarginPercent = 35; // Simplified: would be calculated from actual data
            MonthlyRevenue = TotalMaterialCost * 2.5m; // Simplified estimate
            TotalProfit = MonthlyRevenue - TotalMaterialCost;

            await LoadRecentActivitiesAsync();
            await LoadTopSKUsAsync();

            LogInfo("Dashboard loaded successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error loading dashboard: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshDataAsync()
    {
        await LoadDashboardAsync();
    }

    private async Task LoadRecentActivitiesAsync()
    {
        RecentActivities = new ObservableCollection<string>
        {
            "Batch PROD-2024-001 completed with 95% yield",
            "Recipe v2.1 approved for Donut Coklat",
            "Stock warning: Gula approaching reorder point",
            "Purchase order #PO-2024-1001 received",
            "Quality control pass: 8.5/10 average score"
        };
    }

    private async Task LoadTopSKUsAsync()
    {
        // Simplified: would load from actual reporting service
        TopSKUs = new ObservableCollection<SKUDto>
        {
            new SKUDto { Name = "Donat Polos", Code = "SKU-001", RetailPrice = 5000 },
            new SKUDto { Name = "Donat Coklat", Code = "SKU-002", RetailPrice = 6000 },
            new SKUDto { Name = "Donat Isi Krim", Code = "SKU-003", RetailPrice = 7000 }
        };
    }
}
