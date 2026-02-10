using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class CostCalculationViewModel : BaseViewModel
{
    private readonly IRecipeService _recipeService;
    private readonly ICostCalculationService _costCalculationService;

    [ObservableProperty]
    private ObservableCollection<RecipeDto> recipes = new();

    [ObservableProperty]
    private RecipeDto? selectedRecipe;

    [ObservableProperty]
    private decimal wastePercent;

    [ObservableProperty]
    private decimal packagingCost;

    [ObservableProperty]
    private decimal laborCost;

    [ObservableProperty]
    private decimal overheadCost;

    [ObservableProperty]
    private decimal materialCost;

    [ObservableProperty]
    private decimal totalCost;

    [ObservableProperty]
    private decimal hppPerUnit;

    [ObservableProperty]
    private decimal yieldPerBatch;

    [ObservableProperty]
    private ObservableCollection<IngredientCostLineDto> ingredientCosts = new();

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public CostCalculationViewModel(
        IRecipeService recipeService,
        ICostCalculationService costCalculationService,
        ILogger<CostCalculationViewModel> logger) : base(logger)
    {
        _recipeService = recipeService;
        _costCalculationService = costCalculationService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var recipes = await _recipeService.GetActiveRecipesAsync();
            Recipes = new ObservableCollection<RecipeDto>(recipes);

            StatusMessage = $"Loaded {Recipes.Count} recipe(s)";
        }
        catch (Exception ex)
        {
            SetError($"Error loading recipes: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CalculateAsync()
    {
        try
        {
            if (SelectedRecipe == null)
            {
                SetError("Please select a recipe");
                return;
            }

            IsLoading = true;
            ClearError();

            var breakdown = await _costCalculationService.CalculateRecipeCostBreakdownAsync(
                SelectedRecipe.Id,
                wastePercent: WastePercent,
                packagingCost: PackagingCost,
                laborCost: LaborCost,
                overheadCost: OverheadCost);

            MaterialCost = breakdown.MaterialCost;
            TotalCost = breakdown.TotalCost;
            HppPerUnit = breakdown.HppPerUnit;
            YieldPerBatch = breakdown.YieldPerBatch;
            IngredientCosts = new ObservableCollection<IngredientCostLineDto>(breakdown.IngredientCosts);

            StatusMessage = "Cost calculation completed";
            LogInfo($"Cost calculated for recipe {SelectedRecipe.Name}");
        }
        catch (Exception ex)
        {
            SetError($"Error calculating costs: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ResetInputs()
    {
        WastePercent = 0;
        PackagingCost = 0;
        LaborCost = 0;
        OverheadCost = 0;
    }
}
