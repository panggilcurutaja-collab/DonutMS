using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;
using DonutMS.Data.Repositories;

namespace DonutMS.ViewModels;

// ========== BATCH MANAGEMENT VIEWMODEL ==========
public partial class BatchManagementViewModel : BaseViewModel
{
    private readonly IProductionService _productionService;
    private readonly IRecipeService _recipeService;
    private readonly IInventoryService _inventoryService;

    [ObservableProperty]
    private ObservableCollection<BatchDto> batches = new();

    [ObservableProperty]
    private ObservableCollection<RecipeDto> availableRecipes = new();

    [ObservableProperty]
    private ObservableCollection<StockTransactionDto> batchTransactions = new();

    [ObservableProperty]
    private BatchDto? selectedBatch;

    [ObservableProperty]
    private RecipeDto? selectedRecipe;

    [ObservableProperty]
    private decimal targetYield;

    [ObservableProperty]
    private decimal actualYield;

    [ObservableProperty]
    private decimal wasteQuantityInput;

    [ObservableProperty]
    private string? executionNotes;

    [ObservableProperty]
    private decimal yieldVariance;

    [ObservableProperty]
    private decimal yieldVariancePercent;

    [ObservableProperty]
    private string batchCode = string.Empty;

    [ObservableProperty]
    private string batchStatus = "Planned";

    [ObservableProperty]
    private DateTime fromDate = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime toDate = DateTime.Today;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private int totalBatches;

    [ObservableProperty]
    private int plannedCount;

    [ObservableProperty]
    private int inProgressCount;

    [ObservableProperty]
    private int completedCount;

    [ObservableProperty]
    private bool isJobSheetOpen;

    [ObservableProperty]
    private string jobSheetTitle = "Job Sheet";

    [ObservableProperty]
    private string? qcInspectedBy;

    [ObservableProperty]
    private decimal? qcTaste;

    [ObservableProperty]
    private decimal? qcTexture;

    [ObservableProperty]
    private decimal? qcAppearance;

    [ObservableProperty]
    private decimal? qcAroma;

    [ObservableProperty]
    private string? qcDefectsFound;

    [ObservableProperty]
    private string? qcRemarks;

    public BatchManagementViewModel(
        IProductionService productionService,
        IRecipeService recipeService,
        IInventoryService inventoryService,
        ILogger<BatchManagementViewModel> logger) : base(logger)
    {
        _productionService = productionService;
        _recipeService = recipeService;
        _inventoryService = inventoryService;
    }

    [RelayCommand]
    public async Task LoadBatchesAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var batches = await _productionService.GetBatchesByDateRangeAsync(
                FromDate,
                ToDate.AddDays(1));
            Batches = new ObservableCollection<BatchDto>(batches);

            var recipes = await _recipeService.GetActiveRecipesAsync();
            AvailableRecipes = new ObservableCollection<RecipeDto>(recipes);

            UpdateSummary();
            StatusMessage = $"Loaded {Batches.Count} batches";
            LogInfo("Batches loaded successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error loading batches: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CreateBatchAsync()
    {
        try
        {
            if (SelectedRecipe == null)
            {
                SetError("Please select recipe and enter target yield");
                return;
            }

            var targetYield = TargetYield > 0 ? TargetYield : SelectedRecipe.YieldPerBatch;
            if (targetYield <= 0)
            {
                SetError("Target yield must be greater than 0");
                return;
            }

            IsLoading = true;
            ClearError();

            var batchCode = $"PROD-{DateTime.UtcNow:yyyyMMddHHmm}";
            var dto = new CreateBatchDto
            {
                BatchCode = batchCode,
                RecipeId = SelectedRecipe.Id,
                ProductionDate = DateTime.UtcNow,
                TargetYield = targetYield
            };

            var batch = await _productionService.CreateBatchAsync(dto);
            await LoadBatchesAsync();

            TargetYield = 0;
            SelectedRecipe = null;

            StatusMessage = $"Batch '{batch.BatchCode}' created";
            LogInfo($"Batch '{batch.BatchCode}' created successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error creating batch: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectBatchAsync(BatchDto batch)
    {
        try
        {
            IsLoading = true;
            ClearError();

            SelectedBatch = await _productionService.GetBatchByIdAsync(batch.Id);
            if (SelectedBatch != null)
            {
                ActualYield = SelectedBatch.ActualYield ?? 0;
                WasteQuantityInput = SelectedBatch.WasteQuantity ?? 0;
                ExecutionNotes = SelectedBatch.Notes;
                await LoadBatchTransactionsAsync();
                UpdateVariance();
                LogInfo($"Batch '{batch.BatchCode}' selected");
            }
        }
        catch (Exception ex)
        {
            SetError($"Error selecting batch: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task StartProductionAsync()
    {
        try
        {
            if (SelectedBatch == null)
                return;

            IsLoading = true;
            ClearError();

            await _productionService.StartBatchProductionAsync(SelectedBatch.Id);
            await LoadBatchesAsync();

            StatusMessage = $"Batch '{SelectedBatch.BatchCode}' started";
            LogInfo($"Production started for batch '{SelectedBatch.BatchCode}'");
        }
        catch (Exception ex)
        {
            SetError($"Error starting production: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CompleteBatchAsync()
    {
        try
        {
            if (SelectedBatch == null || ActualYield <= 0)
            {
                SetError("Please enter actual yield");
                return;
            }

            IsLoading = true;
            ClearError();

            await _productionService.CompleteBatchAsync(
                SelectedBatch.Id,
                ActualYield,
                WasteQuantityInput > 0 ? WasteQuantityInput : null,
                ExecutionNotes);
            await LoadBatchesAsync();

            ActualYield = 0;
            WasteQuantityInput = 0;
            ExecutionNotes = null;
            StatusMessage = $"Batch '{SelectedBatch.BatchCode}' completed";
            LogInfo($"Batch '{SelectedBatch.BatchCode}' completed");
        }
        catch (Exception ex)
        {
            SetError($"Error completing batch: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void OpenJobSheet()
    {
        if (SelectedBatch == null)
        {
            SetError("Please select a batch to generate job sheet");
            return;
        }

        JobSheetTitle = $"Job Sheet - {SelectedBatch.BatchCode}";
        IsJobSheetOpen = true;
    }

    [RelayCommand]
    public void CloseJobSheet()
    {
        IsJobSheetOpen = false;
    }

    [RelayCommand]
    public async Task RefreshBatchesAsync()
    {
        await LoadBatchesAsync();
    }

    [RelayCommand]
    public async Task SubmitQualityControlAsync()
    {
        try
        {
            if (SelectedBatch == null)
            {
                SetError("Please select a batch");
                return;
            }

            if (!ValidateQcScore(QcTaste) || !ValidateQcScore(QcTexture) || !ValidateQcScore(QcAppearance) || !ValidateQcScore(QcAroma))
            {
                SetError("QC scores must be between 0 and 10");
                return;
            }

            IsLoading = true;
            ClearError();

            var qcDto = new QualityControlDto
            {
                BatchId = SelectedBatch.Id,
                InspectedBy = QcInspectedBy,
                Taste = QcTaste,
                Texture = QcTexture,
                Appearance = QcAppearance,
                Aroma = QcAroma,
                DefectsFound = QcDefectsFound,
                Remarks = QcRemarks
            };

            var result = await _productionService.AddQualityControlAsync(SelectedBatch.Id, qcDto);
            SelectedBatch = await _productionService.GetBatchByIdAsync(SelectedBatch.Id);

            QcInspectedBy = null;
            QcTaste = null;
            QcTexture = null;
            QcAppearance = null;
            QcAroma = null;
            QcDefectsFound = null;
            QcRemarks = null;

            StatusMessage = result.Passed ? "QC passed and recorded" : "QC recorded (needs review)";
        }
        catch (Exception ex)
        {
            SetError($"Error saving QC: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateSummary()
    {
        TotalBatches = Batches.Count;
        PlannedCount = Batches.Count(b => b.Status == "Planned");
        InProgressCount = Batches.Count(b => b.Status == "In Progress");
        CompletedCount = Batches.Count(b => b.Status == "Completed");
    }

    private async Task LoadBatchTransactionsAsync()
    {
        if (SelectedBatch == null)
        {
            BatchTransactions = new ObservableCollection<StockTransactionDto>();
            return;
        }

        var transactions = await _inventoryService.GetTransactionsByBatchIdAsync(SelectedBatch.Id);
        BatchTransactions = new ObservableCollection<StockTransactionDto>(transactions);
    }

    private static bool ValidateQcScore(decimal? value)
    {
        if (!value.HasValue)
            return false;

        return value.Value >= 0 && value.Value <= 10;
    }

    private void UpdateVariance()
    {
        if (SelectedBatch == null)
        {
            YieldVariance = 0;
            YieldVariancePercent = 0;
            return;
        }

        var target = SelectedBatch.TargetYield;
        var actual = ActualYield > 0 ? ActualYield : SelectedBatch.ActualYield ?? 0;
        YieldVariance = actual - target;
        YieldVariancePercent = target > 0 ? (YieldVariance / target) * 100 : 0;
    }

    partial void OnSelectedBatchChanged(BatchDto? value)
    {
        UpdateVariance();
    }

    partial void OnActualYieldChanged(decimal value)
    {
        UpdateVariance();
    }
}

// ========== PRICING CALCULATOR VIEWMODEL ==========
public partial class PricingCalculatorViewModel : BaseViewModel
{
    private readonly IPricingService _pricingService;
    private readonly ICostCalculationService _costCalculationService;
    private readonly ISKURepository _skuRepository;

    [ObservableProperty]
    private ObservableCollection<SKUDto> skus = new();

    [ObservableProperty]
    private SKUDto? selectedSKU;

    [ObservableProperty]
    private decimal hpp;

    [ObservableProperty]
    private decimal desiredMarginPercent;

    [ObservableProperty]
    private decimal markupPercent;

    [ObservableProperty]
    private decimal markupAmount;

    [ObservableProperty]
    private decimal calculatedSellingPrice;

    [ObservableProperty]
    private decimal grossMargin;

    [ObservableProperty]
    private decimal netMargin;

    [ObservableProperty]
    private decimal breakEvenPrice;

    [ObservableProperty]
    private decimal currentRetailPrice;

    [ObservableProperty]
    private decimal whatIfSellingPrice;

    [ObservableProperty]
    private decimal whatIfGrossMargin;

    [ObservableProperty]
    private decimal whatIfNetMargin;

    [ObservableProperty]
    private ObservableCollection<string> pricingModes = new();

    [ObservableProperty]
    private string selectedPricingMode = "Margin %";

    public PricingCalculatorViewModel(
        IPricingService pricingService,
        ICostCalculationService costCalculationService,
        ISKURepository skuRepository,
        ILogger<PricingCalculatorViewModel> logger) : base(logger)
    {
        _pricingService = pricingService;
        _costCalculationService = costCalculationService;
        _skuRepository = skuRepository;

        PricingModes = new ObservableCollection<string>
        {
            "Margin %",
            "Markup %",
            "Markup Amount"
        };
    }

    [RelayCommand]
    public async Task LoadSKUsAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var skus = await _skuRepository.GetAllAsync();
            Skus = new ObservableCollection<SKUDto>(skus.Select(s => new SKUDto 
            { 
                Id = s.Id, 
                Name = s.Name, 
                Code = s.Code, 
                RetailPrice = s.RetailPrice 
            }));

            LogInfo("SKUs loaded successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error loading SKUs: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectSKUAsync(SKUDto sku)
    {
        try
        {
            IsLoading = true;
            ClearError();

            if (SelectedSKU == null || SelectedSKU.Id != sku.Id)
            {
                SelectedSKU = sku;
            }

            await LoadSkuPricingAsync(sku);

            LogInfo($"SKU '{sku.Name}' selected");
        }
        catch (Exception ex)
        {
            SetError($"Error selecting SKU: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CalculatePriceAsync()
    {
        try
        {
            if (SelectedSKU == null)
            {
                SetError("Please select SKU");
                return;
            }

            IsLoading = true;
            ClearError();

            if (SelectedPricingMode == "Markup %")
            {
                if (MarkupPercent < 0)
                {
                    SetError("Markup % must be 0 or higher");
                    return;
                }

                CalculatedSellingPrice = await _pricingService.CalculateMarkupAsync(Hpp, MarkupPercent);
            }
            else if (SelectedPricingMode == "Markup Amount")
            {
                if (MarkupAmount < 0)
                {
                    SetError("Markup amount must be 0 or higher");
                    return;
                }

                CalculatedSellingPrice = Hpp + MarkupAmount;
            }
            else
            {
                if (DesiredMarginPercent < 0)
                {
                    SetError("Margin % must be 0 or higher");
                    return;
                }

                CalculatedSellingPrice = await _pricingService.CalculateSellingPriceAsync(
                    SelectedSKU.Id,
                    DesiredMarginPercent);
            }

            var (grossMargin, netMargin) = await _pricingService.CalculateMarginsAsync(
                SelectedSKU.Id,
                CalculatedSellingPrice);

            GrossMargin = grossMargin;
            NetMargin = netMargin;

            LogInfo($"Price calculated: Rp {CalculatedSellingPrice:N0}");
        }
        catch (Exception ex)
        {
            SetError($"Error calculating price: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RunWhatIfAsync()
    {
        try
        {
            if (SelectedSKU == null)
            {
                SetError("Please select SKU");
                return;
            }

            if (WhatIfSellingPrice <= 0)
            {
                SetError("What-if price must be greater than 0");
                return;
            }

            IsLoading = true;
            ClearError();

            var (grossMargin, netMargin) = await _pricingService.CalculateMarginsAsync(
                SelectedSKU.Id,
                WhatIfSellingPrice);

            WhatIfGrossMargin = grossMargin;
            WhatIfNetMargin = netMargin;
            LogInfo("What-if simulation completed");
        }
        catch (Exception ex)
        {
            SetError($"Error running what-if: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedSKUChanged(SKUDto? value)
    {
        if (value == null)
            return;

        _ = LoadSkuPricingAsync(value);
    }

    private async Task LoadSkuPricingAsync(SKUDto sku)
    {
        try
        {
            Hpp = await _costCalculationService.CalculateHPPAsync(sku.Id);
            BreakEvenPrice = Hpp;
            CurrentRetailPrice = sku.RetailPrice;
            WhatIfSellingPrice = sku.RetailPrice > 0 ? sku.RetailPrice : Hpp;
        }
        catch (Exception ex)
        {
            SetError($"Error loading SKU pricing: {ex.Message}");
        }
    }
}
