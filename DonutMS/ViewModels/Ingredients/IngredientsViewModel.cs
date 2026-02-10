using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class IngredientsViewModel : BaseViewModel
{
    private readonly IIngredientService _ingredientService;
    private readonly IUnitConversionService _unitService;

    [ObservableProperty]
    private ObservableCollection<IngredientDto> ingredients = new();

    [ObservableProperty]
    private ObservableCollection<UnitDto> availableUnits = new();

    [ObservableProperty]
    private IngredientDto? selectedIngredient;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string filterCategory = "All";

    [ObservableProperty]
    private bool showInactiveOnly = false;

    [ObservableProperty]
    private IngredientDto? editingIngredient;

    [ObservableProperty]
    private bool isDialogOpen = false;

    [ObservableProperty]
    private string dialogTitle = "Add Ingredient";

    public IngredientsViewModel(
        IIngredientService ingredientService,
        IUnitConversionService unitService,
        ILogger<IngredientsViewModel> logger) : base(logger)
    {
        _ingredientService = ingredientService;
        _unitService = unitService;
    }

    [RelayCommand]
    public async Task LoadIngredientsAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var ingredients = await _ingredientService.GetAllIngredientsAsync();
            Ingredients = new ObservableCollection<IngredientDto>(ingredients);

            var units = await _unitService.GetAllUnitsAsync();
            AvailableUnits = new ObservableCollection<UnitDto>(units);

            LogInfo($"Loaded {ingredients.Count()} ingredients");
        }
        catch (Exception ex)
        {
            SetError($"Error loading ingredients: {ex.Message}");
            LogError($"LoadIngredientsAsync failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SearchIngredientsAsync()
    {
        try
        {
            ClearError();

            var allIngredients = await _ingredientService.GetAllIngredientsAsync();
            
            var filtered = allIngredients
                .Where(i => ShowInactiveOnly ? !i.IsActive : i.IsActive)
                .Where(i => string.IsNullOrEmpty(SearchText) || 
                    i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    i.SKU.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Ingredients = new ObservableCollection<IngredientDto>(filtered);
            LogInfo($"Found {filtered.Count} ingredients matching criteria");
        }
        catch (Exception ex)
        {
            SetError($"Error searching ingredients: {ex.Message}");
        }
    }

    [RelayCommand]
    public void OpenAddDialog()
    {
        EditingIngredient = new IngredientDto { IsActive = true };
        DialogTitle = "Add New Ingredient";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public void OpenEditDialog(IngredientDto ingredient)
    {
        if (ingredient == null)
        {
            SetError("Please select an ingredient to edit");
            return;
        }

        EditingIngredient = new IngredientDto
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            SKU = ingredient.SKU,
            Description = ingredient.Description,
            ConsumptionUnitId = ingredient.ConsumptionUnitId,
            PurchaseUnitId = ingredient.PurchaseUnitId,
            MinimumStockLevel = ingredient.MinimumStockLevel,
            ReorderPoint = ingredient.ReorderPoint,
            ReorderQuantity = ingredient.ReorderQuantity,
            ShelfLifeDays = ingredient.ShelfLifeDays,
            IsActive = ingredient.IsActive
        };
        
        DialogTitle = $"Edit Ingredient - {ingredient.Name}";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public async Task SaveIngredientAsync()
    {
        try
        {
            if (EditingIngredient == null)
            {
                SetError("No ingredient data to save");
                return;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(EditingIngredient.Name))
            {
                SetError("Ingredient name is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingIngredient.SKU))
            {
                SetError("SKU is required");
                return;
            }

            IsLoading = true;
            ClearError();

            if (EditingIngredient.Id == 0)
            {
                // Add new ingredient
                var createDto = new CreateIngredientDto
                {
                    Name = EditingIngredient.Name,
                    SKU = EditingIngredient.SKU,
                    Description = EditingIngredient.Description,
                    ConsumptionUnitId = EditingIngredient.ConsumptionUnitId,
                    PurchaseUnitId = EditingIngredient.PurchaseUnitId,
                    MinimumStockLevel = EditingIngredient.MinimumStockLevel,
                    ReorderPoint = EditingIngredient.ReorderPoint,
                    ReorderQuantity = EditingIngredient.ReorderQuantity,
                    ShelfLifeDays = EditingIngredient.ShelfLifeDays
                };

                var newIngredient = await _ingredientService.CreateIngredientAsync(createDto);
                Ingredients.Add(newIngredient);
                LogInfo($"Ingredient '{newIngredient.Name}' created successfully");
            }
            else
            {
                // Update existing ingredient
                var updateDto = new UpdateIngredientDto
                {
                    Name = EditingIngredient.Name,
                    Description = EditingIngredient.Description,
                    ConsumptionUnitId = EditingIngredient.ConsumptionUnitId,
                    PurchaseUnitId = EditingIngredient.PurchaseUnitId,
                    MinimumStockLevel = EditingIngredient.MinimumStockLevel,
                    ReorderPoint = EditingIngredient.ReorderPoint,
                    ReorderQuantity = EditingIngredient.ReorderQuantity,
                    ShelfLifeDays = EditingIngredient.ShelfLifeDays,
                    IsActive = EditingIngredient.IsActive
                };

                await _ingredientService.UpdateIngredientAsync(EditingIngredient.Id, updateDto);
                
                // Refresh the list
                await LoadIngredientsAsync();
                LogInfo($"Ingredient '{EditingIngredient.Name}' updated successfully");
            }

            IsDialogOpen = false;
            EditingIngredient = null;
        }
        catch (Exception ex)
        {
            SetError($"Error saving ingredient: {ex.Message}");
            LogError($"SaveIngredientAsync failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteIngredientAsync(IngredientDto ingredient)
    {
        try
        {
            if (ingredient == null)
            {
                SetError("No ingredient selected");
                return;
            }

            IsLoading = true;
            ClearError();

            await _ingredientService.DeleteIngredientAsync(ingredient.Id);
            Ingredients.Remove(ingredient);
            SelectedIngredient = null;

            LogInfo($"Ingredient '{ingredient.Name}' deleted successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error deleting ingredient: {ex.Message}");
            LogError($"DeleteIngredientAsync failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void CancelDialog()
    {
        IsDialogOpen = false;
        EditingIngredient = null;
        ClearError();
    }

    [RelayCommand]
    public async Task SelectIngredientAsync(IngredientDto ingredient)
    {
        try
        {
            ClearError();
            SelectedIngredient = ingredient;
            LogInfo($"Selected ingredient: {ingredient.Name}");
        }
        catch (Exception ex)
        {
            SetError($"Error selecting ingredient: {ex.Message}");
        }
    }
}
