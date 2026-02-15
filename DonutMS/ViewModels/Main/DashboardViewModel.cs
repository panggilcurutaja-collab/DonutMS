using System.Collections.ObjectModel;
using System.Linq;
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
    private decimal averageProfitPerSku;

    [ObservableProperty]
    private int activeBatchCount;

    [ObservableProperty]
    private decimal targetYield;

    [ObservableProperty]
    private decimal actualYield;

    [ObservableProperty]
    private decimal yieldPercent;

    [ObservableProperty]
    private decimal wastePercent;

    [ObservableProperty]
    private decimal monthlyRevenue;

    [ObservableProperty]
    private decimal totalProfit;

    [ObservableProperty]
    private ObservableCollection<SKUDto> topSKUs = new();

    [ObservableProperty]
    private ObservableCollection<string> recentActivities = new();

    [ObservableProperty]
    private ObservableCollection<InventoryStockDto> lowStockItems = new();

    [ObservableProperty]
    private ObservableCollection<SkuPerformanceDto> topSkuPerformance = new();

    [ObservableProperty]
    private ObservableCollection<SkuPerformanceDto> bottomSkuPerformance = new();

    [ObservableProperty]
    private ObservableCollection<TrendPointDto> weeklyTrend = new();

    [ObservableProperty]
    private ObservableCollection<TrendPointDto> monthlyTrend = new();

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

            var lowStocks = (await _inventoryService.GetLowStockItemsAsync()).ToList();
            LowStockCount = lowStocks.Count;
            LowStockItems = new ObservableCollection<InventoryStockDto>(lowStocks);

            ActiveBatchCount = await _reportingService.GetActiveBatchCountAsync();

            var metrics = await _reportingService.GetProductionMetricsAsync(fromDate, toDate);
            TargetYield = metrics.TargetYield;
            ActualYield = metrics.ActualYield;
            WastePercent = metrics.WastePercent;
            YieldPercent = TargetYield > 0 ? (ActualYield / TargetYield) * 100 : 0m;

            var skuPerformance = (await _reportingService.GetSkuPerformanceAsync()).ToList();
            if (skuPerformance.Any())
            {
                GrossMarginPercent = skuPerformance.Average(s => s.GrossMarginPercent);
                AverageProfitPerSku = skuPerformance.Average(s => s.ProfitPerUnit);
            }
            else
            {
                GrossMarginPercent = 0m;
                AverageProfitPerSku = 0m;
            }

            TopSkuPerformance = new ObservableCollection<SkuPerformanceDto>(
                skuPerformance.OrderByDescending(s => s.ProfitPerUnit).Take(5));
            BottomSkuPerformance = new ObservableCollection<SkuPerformanceDto>(
                skuPerformance.OrderBy(s => s.ProfitPerUnit).Take(5));

            var weekly = (await _reportingService.GetWeeklyMaterialTrendAsync(6)).ToList();
            WeeklyTrend = new ObservableCollection<TrendPointDto>(NormalizeTrend(weekly));

            var monthly = (await _reportingService.GetMonthlyMaterialTrendAsync(6)).ToList();
            MonthlyTrend = new ObservableCollection<TrendPointDto>(NormalizeTrend(monthly));

            MonthlyRevenue = TotalMaterialCost * 2.2m; // Simplified estimate
            TotalProfit = (MonthlyRevenue - TotalMaterialCost) + AverageProfitPerSku;

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

    private static IEnumerable<TrendPointDto> NormalizeTrend(IEnumerable<TrendPointDto> points)
    {
        var list = points.ToList();
        if (!list.Any())
            return list;

        var max = list.Max(p => p.Value);
        if (max <= 0)
        {
            foreach (var point in list)
            {
                point.PercentOfMax = 0;
            }

            return list;
        }

        foreach (var point in list)
        {
            point.PercentOfMax = Math.Clamp((point.Value / max) * 100, 0, 100);
        }

        return list;
    }
}
