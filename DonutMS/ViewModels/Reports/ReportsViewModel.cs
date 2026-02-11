using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class ReportsViewModel : BaseViewModel
{
    private readonly IInventoryService _inventoryService;
    private readonly IIngredientService _ingredientService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IProductionService _productionService;
    private readonly ISKUService _skuService;
    private readonly IRecipeService _recipeService;
    private readonly ICostCalculationService _costCalculationService;
    private readonly IExportService _exportService;
    private readonly ILogger<ReportsViewModel> _logger;


    [ObservableProperty]
    private ObservableCollection<ReportDefinition> reports = new();

    [ObservableProperty]
    private ReportDefinition? selectedReport;

    [ObservableProperty]
    private IEnumerable? currentReportItems;

    [ObservableProperty]
    private DateTime fromDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime toDate = DateTime.Today;

    [ObservableProperty]
    private int expiryWarningDays = 30;

    [ObservableProperty]
    private bool showOnlyExpiring = true;

    [ObservableProperty]
    private string reportTitle = "Reports";

    [ObservableProperty]
    private string reportDescription = string.Empty;

    [ObservableProperty]
    private int resultCount;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string outputFolder = string.Empty;

    [ObservableProperty]
    private string? lastExportPath;

    public ReportsViewModel(
        IInventoryService inventoryService,
        IIngredientService ingredientService,
        IPurchaseOrderService purchaseOrderService,
        IProductionService productionService,
        ISKUService skuService,
        IRecipeService recipeService,
        ICostCalculationService costCalculationService,
        IExportService exportService,
        ILogger<ReportsViewModel> logger) : base(logger)
    {
        _inventoryService = inventoryService;
        _ingredientService = ingredientService;
        _purchaseOrderService = purchaseOrderService;
        _productionService = productionService;
        _skuService = skuService;
        _recipeService = recipeService;
        _costCalculationService = costCalculationService;
        _exportService = exportService;
        _logger = logger;

        OutputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DonutMS",
            "Reports");
    }

    public bool ShowDateRange => SelectedReport?.UsesDateRange ?? false;
    public bool ShowExpiryDays => SelectedReport?.UsesExpiryDays ?? false;

    partial void OnSelectedReportChanged(ReportDefinition? value)
    {
        ReportTitle = value?.Name ?? "Reports";
        ReportDescription = value?.Description ?? string.Empty;
        OnPropertyChanged(nameof(ShowDateRange));
        OnPropertyChanged(nameof(ShowExpiryDays));
    }

    [RelayCommand]
    public async Task LoadReportsAsync()
    {
        try
        {
            ClearError();
            Reports = new ObservableCollection<ReportDefinition>(GetDefaultReports());
            SelectedReport = Reports.FirstOrDefault();

            if (SelectedReport != null)
            {
                await LoadReportAsync();
            }
        }
        catch (Exception ex)
        {
            SetError($"Error loading report definitions: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadReportAsync()
    {
        try
        {
            if (SelectedReport == null)
            {
                SetError("Please select a report");
                return;
            }

            IsLoading = true;
            ClearError();

            IEnumerable items = SelectedReport.Key switch
            {
                ReportKeys.HppReport => await BuildHppReportAsync(),
                ReportKeys.InventoryAging => await BuildInventoryAgingReportAsync(),
                ReportKeys.SupplierPurchase => await BuildSupplierPurchaseReportAsync(),
                ReportKeys.ProductionReport => await BuildProductionReportAsync(),
                ReportKeys.CostingAnalysis => await BuildCostingAnalysisReportAsync(),
                _ => Array.Empty<object>()
            };

            CurrentReportItems = items;
            ResultCount = items.Cast<object>().Count();
            StatusMessage = $"Loaded {ResultCount} row(s) for {SelectedReport.Name}";
            _logger.LogInformation("Report {Report} loaded with {Count} rows", SelectedReport.Name, ResultCount);
        }
        catch (Exception ex)
        {
            SetError($"Error loading report: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ExportCsvAsync()
    {
        await ExportAsync("csv");
    }

    [RelayCommand]
    public async Task ExportJsonAsync()
    {
        await ExportAsync("json");
    }

    [RelayCommand]
    public async Task ExportExcelAsync()
    {
        await ExportAsync("xlsx");
    }

    [RelayCommand]
    public async Task ExportPdfAsync()
    {
        await ExportAsync("pdf");
    }

    [RelayCommand]
    public async Task GenerateTemplateAsync()
    {
        try
        {
            if (SelectedReport == null)
            {
                SetError("Select a report to generate template");
                return;
            }

            var templatePath = Path.Combine(
                OutputFolder,
                $"Template_{SelectedReport.Key}_{DateTime.Now:yyyyMMdd}.csv");

            await _exportService.GenerateTemplateAsync(SelectedReport.ItemType, templatePath);
            LastExportPath = templatePath;
            StatusMessage = $"Template generated: {templatePath}";
        }
        catch (Exception ex)
        {
            SetError($"Template generation failed: {ex.Message}");
        }
    }

    private async Task ExportAsync(string format)
    {
        try
        {
            if (SelectedReport == null)
            {
                SetError("Please select a report first");
                return;
            }

            if (CurrentReportItems == null)
            {
                SetError("No report data to export");
                return;
            }

            var outputPath = Path.Combine(
                OutputFolder,
                $"{SelectedReport.Key}_{DateTime.Now:yyyyMMdd_HHmm}.{ResolveExtension(format)}");

            var title = SelectedReport.Name;

            switch (format)
            {
                case "csv":
                    await _exportService.ExportToCsvAsync(CurrentReportItems, outputPath);
                    break;
                case "json":
                    await _exportService.ExportToJsonAsync(CurrentReportItems, outputPath);
                    break;
                case "xlsx":
                    await _exportService.ExportToExcelAsync(CurrentReportItems, outputPath);
                    break;
                case "pdf":
                    await _exportService.ExportToPdfAsync(CurrentReportItems, outputPath, title);
                    break;
                default:
                    SetError("Unsupported export format");
                    return;
            }

            LastExportPath = outputPath;
            StatusMessage = $"Exported to {outputPath}";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {ex.Message}");
        }
    }

    private static string ResolveExtension(string format)
    {
        return format switch
        {
            "xlsx" => "csv",
            _ => format
        };
    }

    private List<ReportDefinition> GetDefaultReports()
    {
        return new List<ReportDefinition>
        {
            new()
            {
                Key = ReportKeys.HppReport,
                Name = "HPP Report",
                Description = "Harga Pokok Penjualan per SKU dalam periode",
                ItemType = typeof(HppReportItemDto),
                UsesDateRange = true
            },
            new()
            {
                Key = ReportKeys.InventoryAging,
                Name = "Inventory Aging & Expiry",
                Description = "Aging stok dan peringatan kedaluwarsa",
                ItemType = typeof(InventoryAgingReportItemDto),
                UsesExpiryDays = true
            },
            new()
            {
                Key = ReportKeys.SupplierPurchase,
                Name = "Supplier Purchase Report",
                Description = "Ringkasan pembelian per supplier",
                ItemType = typeof(SupplierPurchaseReportItemDto),
                UsesDateRange = true
            },
            new()
            {
                Key = ReportKeys.ProductionReport,
                Name = "Production Target vs Actual",
                Description = "Target vs actual yield dan waste per batch",
                ItemType = typeof(ProductionReportItemDto),
                UsesDateRange = true
            },
            new()
            {
                Key = ReportKeys.CostingAnalysis,
                Name = "Costing Analysis",
                Description = "Analisa biaya per resep",
                ItemType = typeof(CostingAnalysisReportItemDto),
                UsesDateRange = false
            }
        };
    }

    private async Task<List<HppReportItemDto>> BuildHppReportAsync()
    {
        var skus = await _skuService.GetAllSkusAsync();
        var filtered = skus
            .Where(s => s.IsActive)
            .Select(s => new HppReportItemDto
            {
                SKUCode = s.Code,
                SKUName = s.Name,
                RetailPrice = s.RetailPrice,
                HppPerUnit = s.HPP ?? 0m,
                GrossMarginPercent = s.GrossMargin ?? 0m,
                CostEffectiveDate = s.CurrentCost?.EffectiveDate
            })
            .ToList();

        if (ShowDateRange)
        {
            filtered = filtered
                .Where(s => !s.CostEffectiveDate.HasValue ||
                            (s.CostEffectiveDate.Value.Date >= FromDate.Date &&
                             s.CostEffectiveDate.Value.Date <= ToDate.Date))
                .ToList();
        }

        return filtered;
    }

    private async Task<List<InventoryAgingReportItemDto>> BuildInventoryAgingReportAsync()
    {
        var ingredients = await _ingredientService.GetAllIngredientsAsync();
        var results = new List<InventoryAgingReportItemDto>();

        foreach (var ingredient in ingredients)
        {
            var stock = await _inventoryService.GetStockByIngredientIdAsync(ingredient.Id);
            if (stock == null || stock.Batches == null)
                continue;

            foreach (var batch in stock.Batches)
            {
                var daysInStock = (int)(DateTime.Today - batch.ReceiptDate.Date).TotalDays;
                var daysToExpiry = batch.ExpiryDate.HasValue ? batch.DaysToExpiry : int.MaxValue;
                var status = ResolveExpiryStatus(batch.ExpiryDate, daysToExpiry);

                if (ShowOnlyExpiring && status != "Expiring" && status != "Expired")
                    continue;

                results.Add(new InventoryAgingReportItemDto
                {
                    IngredientName = stock.IngredientName,
                    BatchNumber = batch.SupplierBatchNumber,
                    ReceiptDate = batch.ReceiptDate,
                    ExpiryDate = batch.ExpiryDate,
                    DaysInStock = daysInStock,
                    DaysToExpiry = daysToExpiry == int.MaxValue ? 0 : daysToExpiry,
                    AvailableQuantity = batch.AvailableQuantity,
                    UnitCode = stock.UnitCode,
                    AgeCategory = ResolveAgeCategory(daysInStock),
                    ExpiryStatus = status
                });
            }
        }

        return results.OrderBy(r => r.DaysToExpiry).ToList();
    }

    private async Task<List<SupplierPurchaseReportItemDto>> BuildSupplierPurchaseReportAsync()
    {
        var purchaseOrders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
        var filtered = purchaseOrders
            .Where(po => po.OrderDate.Date >= FromDate.Date && po.OrderDate.Date <= ToDate.Date)
            .ToList();

        var grouped = filtered.GroupBy(po => po.SupplierName ?? "Unknown");
        var results = new List<SupplierPurchaseReportItemDto>();

        foreach (var group in grouped)
        {
            var totalOrders = group.Count();
            var totalAmount = group.Sum(po => po.TotalAmount);
            var totalQty = group.SelectMany(po => po.Items ?? Array.Empty<PurchaseOrderItemDto>())
                .Sum(i => i.OrderedQuantity);

            var leadTimes = group
                .Select(po => ResolveLeadTimeDays(po.OrderDate, po.ActualDeliveryDate, po.RequiredDeliveryDate))
                .Where(d => d >= 0)
                .ToList();

            var avgLeadTime = leadTimes.Any() ? leadTimes.Average() : 0;
            var lastOrderDate = group.Max(po => po.OrderDate);

            results.Add(new SupplierPurchaseReportItemDto
            {
                SupplierName = group.Key,
                TotalOrders = totalOrders,
                TotalQuantity = totalQty,
                TotalAmount = totalAmount,
                AverageLeadTimeDays = Math.Round((decimal)avgLeadTime, 1),
                LastOrderDate = lastOrderDate
            });
        }

        return results.OrderByDescending(r => r.TotalAmount).ToList();
    }

    private async Task<List<ProductionReportItemDto>> BuildProductionReportAsync()
    {
        var batches = await _productionService.GetBatchesByDateRangeAsync(FromDate, ToDate.AddDays(1));
        var results = batches.Select(b =>
        {
            var target = b.TargetYield;
            var actual = b.ActualYield ?? 0m;
            var waste = b.WasteQuantity ?? Math.Max(target - actual, 0);
            var wastePercent = target > 0 ? (waste / target) * 100 : 0m;

            return new ProductionReportItemDto
            {
                BatchCode = b.BatchCode,
                RecipeName = b.RecipeName,
                ProductionDate = b.ProductionDate,
                TargetYield = target,
                ActualYield = actual,
                WasteQuantity = waste,
                WastePercent = wastePercent,
                Status = b.Status
            };
        }).ToList();

        return results;
    }

    private async Task<List<CostingAnalysisReportItemDto>> BuildCostingAnalysisReportAsync()
    {
        var recipes = await _recipeService.GetActiveRecipesAsync();
        var results = new List<CostingAnalysisReportItemDto>();

        foreach (var recipe in recipes)
        {
            var breakdown = await _costCalculationService.CalculateRecipeCostBreakdownAsync(
                recipe.Id,
                wastePercent: 0m,
                packagingCost: 0m,
                laborCost: 0m,
                overheadCost: 0m);

            results.Add(new CostingAnalysisReportItemDto
            {
                RecipeName = recipe.Name,
                YieldPerBatch = breakdown.YieldPerBatch,
                MaterialCost = breakdown.MaterialCost,
                PackagingCost = breakdown.PackagingCost,
                LaborCost = breakdown.LaborCost,
                OverheadCost = breakdown.OverheadCost,
                TotalCost = breakdown.TotalCost,
                HppPerUnit = breakdown.HppPerUnit
            });
        }

        return results;
    }

    private string ResolveExpiryStatus(DateTime? expiryDate, int daysToExpiry)
    {
        if (!expiryDate.HasValue)
            return "No Expiry";
        if (daysToExpiry < 0)
            return "Expired";
        if (daysToExpiry <= ExpiryWarningDays)
            return "Expiring";
        return "OK";
    }

    private static string ResolveAgeCategory(int daysInStock)
    {
        if (daysInStock <= 7)
            return "0-7 days";
        if (daysInStock <= 30)
            return "8-30 days";
        if (daysInStock <= 60)
            return "31-60 days";
        return "60+ days";
    }

    private static int ResolveLeadTimeDays(DateTime orderDate, DateTime? actualDelivery, DateTime requiredDelivery)
    {
        var endDate = actualDelivery ?? requiredDelivery;
        return (int)(endDate.Date - orderDate.Date).TotalDays;
    }
}

public class ReportDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Type ItemType { get; set; } = typeof(object);
    public bool UsesDateRange { get; set; }
    public bool UsesExpiryDays { get; set; }
}

public static class ReportKeys
{
    public const string HppReport = "HPP";
    public const string InventoryAging = "InventoryAging";
    public const string SupplierPurchase = "SupplierPurchase";
    public const string ProductionReport = "ProductionReport";
    public const string CostingAnalysis = "CostingAnalysis";
}
