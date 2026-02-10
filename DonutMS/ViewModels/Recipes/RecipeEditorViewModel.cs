using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Data.Entities;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class RecipeEditorViewModel : BaseViewModel
{
    private readonly IRecipeService _recipeService;
    private readonly IIngredientService _ingredientService;
    private readonly IUnitConversionService _unitService;
    private readonly IValidator<Recipe> _recipeValidator;

    [ObservableProperty]
    private RecipeDto? currentRecipe;

    [ObservableProperty]
    private RecipeDto? selectedRecipe;

    [ObservableProperty]
    private ObservableCollection<RecipeIngredientDto> ingredients = new();

    [ObservableProperty]
    private ObservableCollection<RecipeDto> recipes = new();

    [ObservableProperty]
    private ObservableCollection<IngredientDto> availableIngredients = new();

    [ObservableProperty]
    private ObservableCollection<UnitDto> availableUnits = new();

    [ObservableProperty]
    private ObservableCollection<RecipeVersionDto> recipeVersions = new();

    [ObservableProperty]
    private string recipeName = string.Empty;

    [ObservableProperty]
    private string recipeCode = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private decimal yieldPerBatch;

    [ObservableProperty]
    private int selectedYieldUnitId;

    [ObservableProperty]
    private decimal estimatedProductionTime;

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private IngredientDto? selectedIngredient;

    [ObservableProperty]
    private decimal selectedIngredientQty;

    [ObservableProperty]
    private int selectedIngredientUnitId;

    [ObservableProperty]
    private bool showInactiveOnly;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isEditorOpen;

    public RecipeEditorViewModel(
        IRecipeService recipeService,
        IIngredientService ingredientService,
        IUnitConversionService unitService,
        IValidator<Recipe> recipeValidator,
        ILogger<RecipeEditorViewModel> logger) : base(logger)
    {
        _recipeService = recipeService;
        _ingredientService = ingredientService;
        _unitService = unitService;
        _recipeValidator = recipeValidator;
    }

    [RelayCommand]
    public async Task LoadRecipesAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var recipes = await _recipeService.GetAllRecipesAsync();
            var filtered = recipes
                .Where(r => ShowInactiveOnly ? !r.IsActive : r.IsActive)
                .Where(r => string.IsNullOrWhiteSpace(SearchText) ||
                    r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    r.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Recipes = new ObservableCollection<RecipeDto>(filtered);
            await LoadAvailableIngredientsAsync();
            await LoadAvailableUnitsAsync();

            StatusMessage = $"Loaded {Recipes.Count} recipe(s)";
            LogInfo("Recipes loaded successfully");
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
    public void OpenCreateRecipe()
    {
        ClearError();
        SelectedRecipe = null;
        CurrentRecipe = null;
        Ingredients.Clear();
        RecipeVersions.Clear();
        ResetRecipeFields();
        IsEditorOpen = true;
        StatusMessage = "Creating new recipe";
    }

    [RelayCommand]
    public async Task SaveRecipeAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(RecipeName) || string.IsNullOrWhiteSpace(RecipeCode))
            {
                SetError("Recipe name and code are required");
                return;
            }

            if (YieldPerBatch <= 0)
            {
                SetError("Yield per batch must be greater than zero");
                return;
            }

            if (SelectedYieldUnitId <= 0)
            {
                SetError("Please select a yield unit");
                return;
            }

            IsLoading = true;
            ClearError();

            if (CurrentRecipe == null || CurrentRecipe.Id == 0)
            {
                var createDto = new CreateRecipeDto
                {
                    Name = RecipeName,
                    Code = RecipeCode,
                    Description = Description,
                    YieldPerBatch = YieldPerBatch,
                    YieldUnitId = SelectedYieldUnitId,
                    EstimatedProductionTime = EstimatedProductionTime
                };

                CurrentRecipe = await _recipeService.CreateRecipeAsync(createDto);
                Ingredients.Clear();
                RecipeVersions.Clear();
                StatusMessage = $"Recipe '{CurrentRecipe.Name}' created";
                LogInfo($"Recipe '{CurrentRecipe.Name}' created successfully");
            }
            else
            {
                var updateDto = new UpdateRecipeDto
                {
                    Name = RecipeName,
                    Code = RecipeCode,
                    Description = Description,
                    YieldPerBatch = YieldPerBatch,
                    YieldUnitId = SelectedYieldUnitId,
                    EstimatedProductionTime = EstimatedProductionTime,
                    IsActive = IsActive
                };

                CurrentRecipe = await _recipeService.UpdateRecipeAsync(CurrentRecipe.Id, updateDto);
                StatusMessage = $"Recipe '{CurrentRecipe.Name}' updated";
                LogInfo($"Recipe '{CurrentRecipe.Name}' updated successfully");
            }

            await LoadRecipesAsync();
        }
        catch (Exception ex)
        {
            SetError($"Error saving recipe: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectRecipeAsync(RecipeDto recipe)
    {
        try
        {
            IsLoading = true;
            ClearError();

            CurrentRecipe = await _recipeService.GetRecipeByIdAsync(recipe.Id);
            if (CurrentRecipe != null)
            {
                Ingredients = new ObservableCollection<RecipeIngredientDto>(CurrentRecipe.Ingredients);
                await LoadRecipeVersionsAsync(recipe.Id);
                LogInfo($"Recipe '{recipe.Name}' selected");
            }

            IsEditorOpen = true;
        }
        catch (Exception ex)
        {
            SetError($"Error selecting recipe: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddIngredientToRecipeAsync()
    {
        try
        {
            if (CurrentRecipe == null || SelectedIngredient == null)
            {
                SetError("Please select a recipe and ingredient");
                return;
            }

            if (SelectedIngredientQty <= 0)
            {
                SetError("Quantity must be greater than zero");
                return;
            }

            if (SelectedIngredientUnitId <= 0)
            {
                SetError("Please select a unit");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new AddRecipeIngredientDto
            {
                IngredientId = SelectedIngredient.Id,
                QuantityPerBatch = SelectedIngredientQty,
                UnitId = SelectedIngredientUnitId
            };

            await _recipeService.AddIngredientToRecipeAsync(CurrentRecipe.Id, dto);

            CurrentRecipe = await _recipeService.GetRecipeByIdAsync(CurrentRecipe.Id);
            if (CurrentRecipe != null)
            {
                Ingredients = new ObservableCollection<RecipeIngredientDto>(CurrentRecipe.Ingredients);
            }

            var addedIngredientName = SelectedIngredient.Name;
            SelectedIngredient = null;
            SelectedIngredientQty = 0;

            StatusMessage = $"Ingredient '{addedIngredientName}' added";
            LogInfo($"Ingredient '{addedIngredientName}' added to recipe");
        }
        catch (Exception ex)
        {
            SetError($"Error adding ingredient: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RemoveIngredientAsync(RecipeIngredientDto ingredient)
    {
        try
        {
            if (CurrentRecipe == null)
                return;

            IsLoading = true;
            await _recipeService.RemoveIngredientFromRecipeAsync(CurrentRecipe.Id, ingredient.IngredientId);

            CurrentRecipe = await _recipeService.GetRecipeByIdAsync(CurrentRecipe.Id);
            if (CurrentRecipe != null)
            {
                Ingredients = new ObservableCollection<RecipeIngredientDto>(CurrentRecipe.Ingredients);
            }

            LogInfo("Ingredient removed from recipe");
        }
        catch (Exception ex)
        {
            SetError($"Error removing ingredient: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CreateVersionAsync(RecipeDto? recipe)
    {
        try
        {
            if (recipe != null && (CurrentRecipe == null || recipe.Id != CurrentRecipe.Id))
            {
                await SelectRecipeAsync(recipe);
            }

            if (CurrentRecipe == null)
                return;

            IsLoading = true;
            ClearError();

            var dto = new CreateRecipeVersionDto
            {
                ChangeNotes = "Version created",
                EffectiveDate = DateTime.UtcNow,
                YieldPerBatch = YieldPerBatch,
                YieldUnitId = SelectedYieldUnitId > 0 ? SelectedYieldUnitId : CurrentRecipe.YieldUnitId
            };

            var newVersion = await _recipeService.CreateRecipeVersionAsync(CurrentRecipe.Id, dto);
            await LoadRecipeVersionsAsync(CurrentRecipe.Id);

            StatusMessage = $"Recipe version {newVersion.VersionNumber} created";
            LogInfo("Recipe version created successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error creating version: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteRecipeAsync(RecipeDto recipe)
    {
        try
        {
            if (recipe == null)
            {
                SetError("No recipe selected");
                return;
            }

            IsLoading = true;
            ClearError();

            await _recipeService.DeleteRecipeAsync(recipe.Id);
            await LoadRecipesAsync();

            CurrentRecipe = null;
            Ingredients.Clear();
            RecipeVersions.Clear();

            StatusMessage = $"Recipe '{recipe.Name}' deleted";
            LogInfo($"Recipe '{recipe.Name}' deleted successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error deleting recipe: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void CloseEditor()
    {
        IsEditorOpen = false;
        ClearError();
    }

    [RelayCommand]
    public async Task RollbackToVersionAsync(RecipeVersionDto version)
    {
        try
        {
            if (CurrentRecipe == null)
            {
                SetError("Select a recipe before rollback");
                return;
            }

            IsLoading = true;
            ClearError();

            await _recipeService.RollbackToVersionAsync(CurrentRecipe.Id, version.VersionNumber);
            await LoadRecipeVersionsAsync(CurrentRecipe.Id);
            StatusMessage = $"Rolled back to version {version.VersionNumber}";
        }
        catch (Exception ex)
        {
            SetError($"Error rolling back version: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAvailableIngredientsAsync()
    {
        var ingredients = await _ingredientService.GetAllIngredientsAsync();
        AvailableIngredients = new ObservableCollection<IngredientDto>(ingredients);
    }

    private async Task LoadAvailableUnitsAsync()
    {
        var units = await _unitService.GetAllUnitsAsync();
        AvailableUnits = new ObservableCollection<UnitDto>(units);
        EnsureDefaultYieldUnit();
    }

    private async Task LoadRecipeVersionsAsync(int recipeId)
    {
        var versions = await _recipeService.GetRecipeVersionHistoryAsync(recipeId);
        RecipeVersions = new ObservableCollection<RecipeVersionDto>(versions);
    }

    private void ResetRecipeFields()
    {
        RecipeName = string.Empty;
        RecipeCode = string.Empty;
        Description = string.Empty;
        YieldPerBatch = 0;
        EstimatedProductionTime = 0;
        IsActive = true;
        EnsureDefaultYieldUnit();
    }

    private void EnsureDefaultYieldUnit()
    {
        if (SelectedYieldUnitId > 0 || AvailableUnits.Count == 0)
            return;

        var defaultUnit = AvailableUnits.FirstOrDefault(u => u.Code.Equals("pcs", StringComparison.OrdinalIgnoreCase));
        SelectedYieldUnitId = defaultUnit?.Id ?? AvailableUnits.First().Id;
    }

    partial void OnSelectedIngredientChanged(IngredientDto? value)
    {
        if (value != null && value.ConsumptionUnitId > 0)
        {
            SelectedIngredientUnitId = value.ConsumptionUnitId;
        }
    }

    partial void OnCurrentRecipeChanged(RecipeDto? value)
    {
        if (value == null)
        {
            ResetRecipeFields();
            return;
        }

        RecipeName = value.Name;
        RecipeCode = value.Code;
        Description = value.Description ?? string.Empty;
        YieldPerBatch = value.YieldPerBatch;
        SelectedYieldUnitId = value.YieldUnitId;
        EstimatedProductionTime = value.EstimatedProductionTime;
        IsActive = value.IsActive;
    }
}
