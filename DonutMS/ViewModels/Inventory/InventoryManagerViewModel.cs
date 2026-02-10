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
    private InventoryStockDto? selectedStock;

    [ObservableProperty]
    private IngredientDto? selectedIngredient;

    [ObservableProperty]
    private decimal transactionQuantity;

    [ObservableProperty]
    private string transactionType = "In";

    [ObservableProperty]
    private int lowStockCount;

    [ObservableProperty]
    private decimal totalInventoryValue;

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
                if (stock != null)
                    stocks.Add(stock);
            }

            InventoryStocks = new ObservableCollection<InventoryStockDto>(stocks);
            await LoadLowStockCountAsync();
            CalculateTotalInventoryValue();

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

            SelectedStock = stock;
            SelectedStockBatches = new ObservableCollection<StockBatchDto>(stock.Batches);

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
                SelectedStock.UnitId);

            await LoadInventoryAsync();
            TransactionQuantity = 0;

            LogInfo($"Stock in: {TransactionQuantity} units recorded");
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
                SelectedStock.UnitId);

            await LoadInventoryAsync();
            TransactionQuantity = 0;

            LogInfo($"Stock out: {TransactionQuantity} units recorded");
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

            var expiringBatches = await _inventoryService.GetExpiringStockAsync(30);
            if (expiringBatches.Any())
            {
                SetError($"?? {expiringBatches.Count()} batches expiring in 30 days");
            }
            else
            {
                LogInfo("No batches expiring in next 30 days");
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

    private async Task LoadLowStockCountAsync()
    {
        var lowStocks = await _inventoryService.GetLowStockItemsAsync();
        LowStockCount = lowStocks.Count();
    }

    private void CalculateTotalInventoryValue()
    {
        TotalInventoryValue = InventoryStocks
            .Sum(s => s.Quantity * (s.IngredientName == "Telur" ? 2000 : s.Quantity > 100 ? 1000 : 500));
    }
}
