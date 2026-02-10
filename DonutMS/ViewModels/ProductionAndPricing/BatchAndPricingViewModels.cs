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
    private BatchDto? selectedBatch;

    [ObservableProperty]
    private RecipeDto? selectedRecipe;

    [ObservableProperty]
    private decimal targetYield;

    [ObservableProperty]
    private decimal actualYield;

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

            await _productionService.CompleteBatchAsync(SelectedBatch.Id, ActualYield);
            await LoadBatchesAsync();

            ActualYield = 0;
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

    private void UpdateSummary()
    {
        TotalBatches = Batches.Count;
        PlannedCount = Batches.Count(b => b.Status == "Planned");
        InProgressCount = Batches.Count(b => b.Status == "In Progress");
        CompletedCount = Batches.Count(b => b.Status == "Completed");
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
    private decimal calculatedSellingPrice;

    [ObservableProperty]
    private decimal grossMargin;

    [ObservableProperty]
    private decimal netMargin;

    [ObservableProperty]
    private decimal breakEvenPrice;

    public PricingCalculatorViewModel(
        IPricingService pricingService,
        ICostCalculationService costCalculationService,
        ISKURepository skuRepository,
        ILogger<PricingCalculatorViewModel> logger) : base(logger)
    {
        _pricingService = pricingService;
        _costCalculationService = costCalculationService;
        _skuRepository = skuRepository;
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

            SelectedSKU = sku;
            Hpp = await _costCalculationService.CalculateHPPAsync(sku.Id);
            BreakEvenPrice = Hpp;

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
            if (SelectedSKU == null || DesiredMarginPercent < 0)
            {
                SetError("Please select SKU and enter margin");
                return;
            }

            IsLoading = true;
            ClearError();

            CalculatedSellingPrice = await _pricingService.CalculateSellingPriceAsync(
                SelectedSKU.Id,
                DesiredMarginPercent);

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
}
