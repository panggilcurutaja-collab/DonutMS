using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class SubstitutionManagerViewModel : BaseViewModel
{
    private readonly IRecipeService _recipeService;
    private readonly IIngredientService _ingredientService;
    private readonly IUnitConversionService _unitConversionService;

    [ObservableProperty]
    private ObservableCollection<IngredientDto> ingredients = new();

    [ObservableProperty]
    private ObservableCollection<RecipeSubstitutionDto> substitutions = new();

    [ObservableProperty]
    private IngredientDto? selectedOriginalIngredient;

    [ObservableProperty]
    private IngredientDto? selectedSubstituteIngredient;

    [ObservableProperty]
    private decimal substitutionRatio = 1m;

    [ObservableProperty]
    private decimal costImpact;

    [ObservableProperty]
    private decimal costImpactPercent;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SubstitutionManagerViewModel(
        IRecipeService recipeService,
        IIngredientService ingredientService,
        IUnitConversionService unitConversionService,
        ILogger<SubstitutionManagerViewModel> logger) : base(logger)
    {
        _recipeService = recipeService;
        _ingredientService = ingredientService;
        _unitConversionService = unitConversionService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var ingredients = await _ingredientService.GetAllIngredientsAsync();
            Ingredients = new ObservableCollection<IngredientDto>(ingredients.Where(i => i.IsActive));

            await LoadSubstitutionsAsync();
            StatusMessage = $"Loaded {Substitutions.Count} substitution rule(s)";
        }
        catch (Exception ex)
        {
            SetError($"Error loading substitutions: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadSubstitutionsAsync()
    {
        var substitutions = await _recipeService.GetAllSubstitutionsAsync();
        Substitutions = new ObservableCollection<RecipeSubstitutionDto>(substitutions);
    }

    [RelayCommand]
    public async Task CreateSubstitutionAsync()
    {
        try
        {
            if (SelectedOriginalIngredient == null || SelectedSubstituteIngredient == null)
            {
                SetError("Please select both original and substitute ingredients");
                return;
            }

            if (SelectedOriginalIngredient.Id == SelectedSubstituteIngredient.Id)
            {
                SetError("Original and substitute ingredients must be different");
                return;
            }

            if (SubstitutionRatio <= 0)
            {
                SetError("Substitution ratio must be greater than zero");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new CreateRecipeSubstitutionDto
            {
                OriginalIngredientId = SelectedOriginalIngredient.Id,
                SubstituteIngredientId = SelectedSubstituteIngredient.Id,
                SubstitutionRatio = SubstitutionRatio,
                CostImpact = CostImpact
            };

            await _recipeService.CreateSubstitutionAsync(dto);
            await LoadSubstitutionsAsync();

            StatusMessage = "Substitution rule saved";
            LogInfo("Substitution rule created");
        }
        catch (Exception ex)
        {
            SetError($"Error creating substitution: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ApproveSubstitutionAsync(RecipeSubstitutionDto substitution)
    {
        try
        {
            IsLoading = true;
            ClearError();

            await _recipeService.ApproveSubstitutionAsync(
                substitution.OriginalIngredientId,
                substitution.SubstituteIngredientId);

            await LoadSubstitutionsAsync();
            StatusMessage = "Substitution approved";
        }
        catch (Exception ex)
        {
            SetError($"Error approving substitution: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RecalculateCostImpactAsync()
    {
        try
        {
            if (SelectedOriginalIngredient == null || SelectedSubstituteIngredient == null)
            {
                CostImpact = 0;
                CostImpactPercent = 0;
                return;
            }

            var original = await _ingredientService.GetIngredientByIdAsync(SelectedOriginalIngredient.Id);
            var substitute = await _ingredientService.GetIngredientByIdAsync(SelectedSubstituteIngredient.Id);

            if (original?.CurrentPrice == null || substitute?.CurrentPrice == null)
            {
                CostImpact = 0;
                CostImpactPercent = 0;
                StatusMessage = "Price data missing for cost impact calculation";
                return;
            }

            var originalUnitCost = await GetUnitCostAsync(original);
            var substituteUnitCost = await GetUnitCostAsync(substitute);

            if (originalUnitCost == null || substituteUnitCost == null)
            {
                CostImpact = 0;
                CostImpactPercent = 0;
                StatusMessage = "Unable to calculate cost impact (unit conversion issue)";
                return;
            }

            CostImpact = (substituteUnitCost.Value * SubstitutionRatio) - originalUnitCost.Value;
            CostImpactPercent = originalUnitCost.Value == 0
                ? 0
                : (CostImpact / originalUnitCost.Value) * 100;

            StatusMessage = "Cost impact calculated";
        }
        catch (Exception ex)
        {
            SetError($"Error calculating cost impact: {ex.Message}");
        }
    }

    private async Task<decimal?> GetUnitCostAsync(IngredientDto ingredient)
    {
        var price = ingredient.CurrentPrice;
        if (price == null)
            return null;

        if (price.UnitId == ingredient.ConsumptionUnitId)
            return price.Price;

        try
        {
            var convertedQuantity = await _unitConversionService.ConvertAsync(1m, price.UnitId, ingredient.ConsumptionUnitId);
            if (convertedQuantity <= 0)
                return null;

            return price.Price / convertedQuantity;
        }
        catch
        {
            return null;
        }
    }

    partial void OnSelectedOriginalIngredientChanged(IngredientDto? value)
    {
        _ = RecalculateCostImpactAsync();
    }

    partial void OnSelectedSubstituteIngredientChanged(IngredientDto? value)
    {
        _ = RecalculateCostImpactAsync();
    }

    partial void OnSubstitutionRatioChanged(decimal value)
    {
        _ = RecalculateCostImpactAsync();
    }
}
