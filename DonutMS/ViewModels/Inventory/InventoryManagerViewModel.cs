using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class InventoryManagerViewModel : BaseViewModel
{
    private readonly IInventoryService _inventoryService;
    private readonly IIngredientService _ingredientService;

    [ObservableProperty]
    private ObservableCollection<InventoryStockDto> inventoryStocks = new();

    [ObservableProperty]
    private ObservableCollection<IngredientDto> ingredients = new();

    [ObservableProperty]
    private ObservableCollection<StockBatchDto> selectedStockBatches = new();

    [ObservableProperty]
    private ObservableCollection<StockTransactionDto> selectedTransactions = new();

    [ObservableProperty]
    private InventoryStockDto? selectedStock;

    [ObservableProperty]
    private IngredientDto? selectedIngredient;

    [ObservableProperty]
    private decimal transactionQuantity;

    [ObservableProperty]
    private string transactionType = "In";

    [ObservableProperty]
    private string consumptionMethod = "FEFO";

    [ObservableProperty]
    private StockBatchDto? selectedBatch;

    [ObservableProperty]
    private string? stockInBatchNumber;

    [ObservableProperty]
    private DateTime stockInReceiptDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? stockInExpiryDate = DateTime.Today.AddDays(30);

    [ObservableProperty]
    private string? transactionReference;

    [ObservableProperty]
    private int lowStockCount;

    [ObservableProperty]
    private decimal totalInventoryValue;

    [ObservableProperty]
    private int expiringBatchCount;

    [ObservableProperty]
    private int expiringWithinDays = 30;

    [ObservableProperty]
    private DateTime transactionFromDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime transactionToDate = DateTime.Today;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public InventoryManagerViewModel(
        IInventoryService inventoryService,
        IIngredientService ingredientService,
        ILogger<InventoryManagerViewModel> logger) : base(logger)
    {
        _inventoryService = inventoryService;
        _ingredientService = ingredientService;
    }

    [RelayCommand]
    public async Task LoadInventoryAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var ingredients = await _ingredientService.GetAllIngredientsAsync();
            Ingredients = new ObservableCollection<IngredientDto>(ingredients);

            var stocks = new List<InventoryStockDto>();
            foreach (var ingredient in ingredients)
            {
                var stock = await _inventoryService.GetStockByIngredientIdAsync(ingredient.Id);
                if (stock == null)
                {
                    stock = new InventoryStockDto
                    {
                        IngredientId = ingredient.Id,
                        IngredientName = ingredient.Name,
                        IngredientSKU = ingredient.SKU,
                        UnitId = ingredient.ConsumptionUnitId,
                        UnitCode = ingredient.ConsumptionUnitCode,
                        Quantity = 0,
                        ReservedQuantity = 0,
                        AvailableQuantity = 0,
                        LastUpdated = DateTime.UtcNow,
                        MinimumStockLevel = ingredient.MinimumStockLevel,
                        ReorderPoint = ingredient.ReorderPoint,
                        ShelfLifeDays = ingredient.ShelfLifeDays
                    };
                }

                stocks.Add(stock);
            }

            ApplyComputedFields(stocks);
            InventoryStocks = new ObservableCollection<InventoryStockDto>(stocks);
            await LoadLowStockCountAsync();
            CalculateTotalInventoryValue();
            StatusMessage = $"Loaded {InventoryStocks.Count} stock items";

            LogInfo("Inventory loaded successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error loading inventory: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectStockAsync(InventoryStockDto stock)
    {
        try
        {
            IsLoading = true;
            ClearError();

            if (SelectedStock != stock)
                SelectedStock = stock;
            SelectedStockBatches = new ObservableCollection<StockBatchDto>(stock.Batches ?? Array.Empty<StockBatchDto>());
            SelectedBatch = null;

            await LoadTransactionsAsync();

            LogInfo($"Stock for {stock.IngredientName} selected");
        }
        catch (Exception ex)
        {
            SetError($"Error selecting stock: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedStockChanged(InventoryStockDto? value)
    {
        if (value != null)
        {
            _ = SelectStockAsync(value);
        }
    }

    [RelayCommand]
    public async Task AddStockInAsync()
    {
        try
        {
            if (SelectedStock == null || TransactionQuantity <= 0)
            {
                SetError("Please select stock and enter quantity");
                return;
            }

            IsLoading = true;
            ClearError();

            await _inventoryService.AddStockInAsync(
                SelectedStock.IngredientId,
                TransactionQuantity,
                SelectedStock.UnitId,
                StockInReceiptDate,
                StockInExpiryDate,
                StockInBatchNumber,
                TransactionReference);

            await LoadInventoryAsync();
            var recorded = TransactionQuantity;
            TransactionQuantity = 0;
            StockInBatchNumber = null;
            TransactionReference = null;

            StatusMessage = $"Stock in recorded: {recorded:N2}";
            LogInfo($"Stock in: {recorded} units recorded");
        }
        catch (Exception ex)
        {
            SetError($"Error recording stock in: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RemoveStockOutAsync()
    {
        try
        {
            if (SelectedStock == null || TransactionQuantity <= 0)
            {
                SetError("Please select stock and enter quantity");
                return;
            }

            IsLoading = true;
            ClearError();

            await _inventoryService.RemoveStockOutAsync(
                SelectedStock.IngredientId,
                TransactionQuantity,
                SelectedStock.UnitId,
                ConsumptionMethod.Equals("FEFO", StringComparison.OrdinalIgnoreCase),
                SelectedBatch?.Id,
                TransactionReference);

            await LoadInventoryAsync();
            var recorded = TransactionQuantity;
            TransactionQuantity = 0;
            SelectedBatch = null;
            TransactionReference = null;

            StatusMessage = $"Stock out recorded: {recorded:N2}";
            LogInfo($"Stock out: {recorded} units recorded");
        }
        catch (Exception ex)
        {
            SetError($"Error recording stock out: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CheckExpiringStockAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var expiringBatches = await _inventoryService.GetExpiringStockAsync(ExpiringWithinDays);
            if (expiringBatches.Any())
            {
                ExpiringBatchCount = expiringBatches.Count();
                StatusMessage = $"{ExpiringBatchCount} batches expiring in {ExpiringWithinDays} days";
            }
            else
            {
                ExpiringBatchCount = 0;
                StatusMessage = $"No batches expiring in next {ExpiringWithinDays} days";
            }
        }
        catch (Exception ex)
        {
            SetError($"Error checking expiring stock: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshInventoryAsync()
    {
        await LoadInventoryAsync();
    }

    [RelayCommand]
    public async Task LoadTransactionsAsync()
    {
        try
        {
            if (SelectedStock == null)
            {
                SelectedTransactions = new ObservableCollection<StockTransactionDto>();
                return;
            }

            var transactions = await _inventoryService.GetTransactionsAsync(
                SelectedStock.IngredientId,
                TransactionFromDate,
                TransactionToDate.AddDays(1));

            SelectedTransactions = new ObservableCollection<StockTransactionDto>(transactions);
        }
        catch (Exception ex)
        {
            SetError($"Error loading transactions: {ex.Message}");
        }
    }

    private async Task LoadLowStockCountAsync()
    {
        var lowStocks = await _inventoryService.GetLowStockItemsAsync();
        LowStockCount = lowStocks.Count();
    }

    private void CalculateTotalInventoryValue()
    {
        decimal total = 0m;
        foreach (var stock in InventoryStocks)
        {
            var ingredient = Ingredients.FirstOrDefault(i => i.Id == stock.IngredientId);
            var price = ingredient?.CurrentPrice?.Price ?? 0m;
            if (price > 0)
            {
                total += stock.Quantity * price;
            }
        }

        TotalInventoryValue = total;
    }

    private static void ApplyComputedFields(IEnumerable<InventoryStockDto> stocks)
    {
        foreach (var stock in stocks)
        {
            var batches = stock.Batches ?? Enumerable.Empty<StockBatchDto>();
            var nextBatch = batches
                .Where(b => b.ExpiryDate.HasValue && b.AvailableQuantity > 0)
                .OrderBy(b => b.ExpiryDate)
                .FirstOrDefault();

            stock.NextExpiryDate = nextBatch?.ExpiryDate;
            stock.NextExpiryDays = nextBatch?.DaysToExpiry;
            stock.IsLowStock = stock.MinimumStockLevel > 0 && stock.AvailableQuantity <= stock.MinimumStockLevel;
        }
    }
}
