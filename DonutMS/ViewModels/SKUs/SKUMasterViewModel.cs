using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class SKUMasterViewModel : BaseViewModel
{
    private readonly ISKUService _skuService;
    private readonly IRecipeService _recipeService;
    private readonly ICostCalculationService _costCalculationService;

    [ObservableProperty]
    private ObservableCollection<SKUDto> skus = new();

    [ObservableProperty]
    private ObservableCollection<RecipeDto> availableRecipes = new();

    [ObservableProperty]
    private ObservableCollection<SKUCostDto> costHistory = new();

    [ObservableProperty]
    private ObservableCollection<AllergenSelection> availableAllergens = new();

    [ObservableProperty]
    private SKUDto? selectedSku;

    [ObservableProperty]
    private SKUDto? editingSku;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool showInactiveOnly;

    [ObservableProperty]
    private bool isEditorOpen;

    [ObservableProperty]
    private string dialogTitle = "Add SKU";

    [ObservableProperty]
    private NutritionalInfoDto nutrition = new();

    [ObservableProperty]
    private decimal packagingCostInput;

    [ObservableProperty]
    private decimal laborCostInput;

    [ObservableProperty]
    private decimal overheadCostInput;

    [ObservableProperty]
    private decimal materialCostPreview;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SKUMasterViewModel(
        ISKUService skuService,
        IRecipeService recipeService,
        ICostCalculationService costCalculationService,
        ILogger<SKUMasterViewModel> logger) : base(logger)
    {
        _skuService = skuService;
        _recipeService = recipeService;
        _costCalculationService = costCalculationService;
    }

    [RelayCommand]
    public async Task LoadSkusAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var skus = await _skuService.GetAllSkusAsync();
            var filtered = skus
                .Where(s => ShowInactiveOnly ? !s.IsActive : s.IsActive)
                .Where(s => string.IsNullOrWhiteSpace(SearchText) ||
                    s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Skus = new ObservableCollection<SKUDto>(filtered);

            var recipes = await _recipeService.GetActiveRecipesAsync();
            AvailableRecipes = new ObservableCollection<RecipeDto>(recipes);

            StatusMessage = $"Loaded {Skus.Count} SKU(s)";
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
    public async Task SearchSkusAsync()
    {
        await LoadSkusAsync();
    }

    [RelayCommand]
    public async Task OpenAddDialogAsync()
    {
        ClearError();
        EditingSku = new SKUDto { IsActive = true };
        DialogTitle = "Add New SKU";
        IsEditorOpen = true;

        await LoadAllergensForSkuAsync(0);
        Nutrition = new NutritionalInfoDto();
        CostHistory = new ObservableCollection<SKUCostDto>();
        ResetCostInputs();
    }

    [RelayCommand]
    public async Task OpenEditDialogAsync(SKUDto sku)
    {
        if (sku == null)
        {
            SetError("Please select a SKU to edit");
            return;
        }

        try
        {
            IsLoading = true;
            ClearError();

            var detail = await _skuService.GetSkuByIdAsync(sku.Id);
            if (detail == null)
            {
                SetError("SKU not found");
                return;
            }

            EditingSku = new SKUDto
            {
                Id = detail.Id,
                Name = detail.Name,
                Code = detail.Code,
                Description = detail.Description,
                Category = detail.Category,
                RecipeId = detail.RecipeId,
                RecipeName = detail.RecipeName,
                RetailPrice = detail.RetailPrice,
                IsActive = detail.IsActive,
                CurrentCost = detail.CurrentCost,
                HPP = detail.HPP,
                GrossMargin = detail.GrossMargin
            };

            DialogTitle = $"Edit SKU - {detail.Name}";
            IsEditorOpen = true;

            var costHistory = await _skuService.GetSkuCostHistoryAsync(detail.Id);
            CostHistory = new ObservableCollection<SKUCostDto>(costHistory);

            await LoadAllergensForSkuAsync(detail.Id);

            var nutrition = await _skuService.GetNutritionalInfoAsync(detail.Id);
            Nutrition = nutrition ?? new NutritionalInfoDto();

            ResetCostInputs();
        }
        catch (Exception ex)
        {
            SetError($"Error opening SKU editor: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveSkuAsync()
    {
        try
        {
            if (EditingSku == null)
            {
                SetError("No SKU data to save");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingSku.Name) || string.IsNullOrWhiteSpace(EditingSku.Code))
            {
                SetError("SKU name and code are required");
                return;
            }

            if (EditingSku.RetailPrice < 0)
            {
                SetError("Retail price cannot be negative");
                return;
            }

            IsLoading = true;
            ClearError();

            if (EditingSku.Id == 0)
            {
                var createDto = new CreateSKUDto
                {
                    Name = EditingSku.Name,
                    Code = EditingSku.Code,
                    Description = EditingSku.Description,
                    Category = EditingSku.Category,
                    RecipeId = EditingSku.RecipeId,
                    RetailPrice = EditingSku.RetailPrice,
                    IsActive = EditingSku.IsActive
                };

                var created = await _skuService.CreateSkuAsync(createDto);
                Skus.Add(created);
                StatusMessage = $"SKU '{created.Name}' created";
            }
            else
            {
                var updateDto = new UpdateSKUDto
                {
                    Name = EditingSku.Name,
                    Code = EditingSku.Code,
                    Description = EditingSku.Description,
                    Category = EditingSku.Category,
                    RecipeId = EditingSku.RecipeId,
                    RetailPrice = EditingSku.RetailPrice,
                    IsActive = EditingSku.IsActive
                };

                await _skuService.UpdateSkuAsync(EditingSku.Id, updateDto);
                await LoadSkusAsync();
                StatusMessage = $"SKU '{EditingSku.Name}' updated";
            }

            IsEditorOpen = false;
            EditingSku = null;
        }
        catch (Exception ex)
        {
            SetError($"Error saving SKU: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSkuAsync(SKUDto sku)
    {
        try
        {
            if (sku == null)
            {
                SetError("No SKU selected");
                return;
            }

            IsLoading = true;
            ClearError();

            await _skuService.DeleteSkuAsync(sku.Id);
            Skus.Remove(sku);
            SelectedSku = null;

            StatusMessage = $"SKU '{sku.Name}' deleted";
        }
        catch (Exception ex)
        {
            SetError($"Error deleting SKU: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void CancelDialog()
    {
        IsEditorOpen = false;
        EditingSku = null;
        ClearError();
    }

    [RelayCommand]
    public async Task AddCostVersionAsync()
    {
        try
        {
            if (EditingSku == null || EditingSku.Id == 0)
            {
                SetError("Save SKU before adding cost version");
                return;
            }

            if (!EditingSku.RecipeId.HasValue)
            {
                SetError("Link a recipe to calculate material cost");
                return;
            }

            IsLoading = true;
            ClearError();

            MaterialCostPreview = await _costCalculationService.CalculateMaterialCostAsync(EditingSku.RecipeId.Value);

            var createDto = new CreateSKUCostDto
            {
                SKUId = EditingSku.Id,
                MaterialCost = MaterialCostPreview,
                PackagingCost = PackagingCostInput,
                LaborCost = LaborCostInput,
                OverheadCost = OverheadCostInput
            };

            await _skuService.AddSkuCostVersionAsync(EditingSku.Id, createDto);
            var history = await _skuService.GetSkuCostHistoryAsync(EditingSku.Id);
            CostHistory = new ObservableCollection<SKUCostDto>(history);

            await LoadSkusAsync();
            StatusMessage = "Cost version added";
            ResetCostInputs();
        }
        catch (Exception ex)
        {
            SetError($"Error adding cost version: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveAllergensAsync()
    {
        try
        {
            if (EditingSku == null || EditingSku.Id == 0)
            {
                SetError("Save SKU before editing allergens");
                return;
            }

            IsLoading = true;
            ClearError();

            var selected = AvailableAllergens
                .Where(a => a.IsSelected)
                .Select(a => new SKUAllergenDto
                {
                    SKUId = EditingSku.Id,
                    AllergenId = a.Id,
                    MayContainTrace = a.MayContainTrace
                })
                .ToList();

            await _skuService.SaveSkuAllergensAsync(EditingSku.Id, selected);
            StatusMessage = "Allergens updated";
        }
        catch (Exception ex)
        {
            SetError($"Error saving allergens: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveNutritionAsync()
    {
        try
        {
            if (EditingSku == null || EditingSku.Id == 0)
            {
                SetError("Save SKU before editing nutrition");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new UpsertNutritionalInfoDto
            {
                Calories = Nutrition.Calories,
                Protein = Nutrition.Protein,
                Fat = Nutrition.Fat,
                Carbohydrates = Nutrition.Carbohydrates,
                Fiber = Nutrition.Fiber,
                Sugar = Nutrition.Sugar,
                Sodium = Nutrition.Sodium,
                ServingSize = Nutrition.ServingSize,
                ServingsPerPackage = Nutrition.ServingsPerPackage,
                Notes = Nutrition.Notes
            };

            Nutrition = await _skuService.UpsertNutritionalInfoAsync(EditingSku.Id, dto);
            StatusMessage = "Nutrition info saved";
        }
        catch (Exception ex)
        {
            SetError($"Error saving nutrition info: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAllergensForSkuAsync(int skuId)
    {
        var allAllergens = await _skuService.GetAllergensAsync();
        var skuAllergens = skuId > 0
            ? await _skuService.GetSkuAllergensAsync(skuId)
            : Array.Empty<SKUAllergenDto>();

        var selection = allAllergens.Select(a =>
        {
            var linked = skuAllergens.FirstOrDefault(sa => sa.AllergenId == a.Id);
            return new AllergenSelection
            {
                Id = a.Id,
                Name = a.Name,
                Category = a.Category,
                IsSelected = linked != null,
                MayContainTrace = linked?.MayContainTrace ?? false
            };
        });

        AvailableAllergens = new ObservableCollection<AllergenSelection>(selection);
    }

    private void ResetCostInputs()
    {
        PackagingCostInput = 0;
        LaborCostInput = 0;
        OverheadCostInput = 0;
        MaterialCostPreview = 0;
    }
}

public partial class AllergenSelection : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? category;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool mayContainTrace;
}
