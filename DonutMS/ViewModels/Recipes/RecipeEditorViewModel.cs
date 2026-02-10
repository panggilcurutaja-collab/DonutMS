using System.Collections.ObjectModel;
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
    private readonly IValidator<Recipe> _recipeValidator;

    [ObservableProperty]
    private RecipeDto? currentRecipe;

    [ObservableProperty]
    private ObservableCollection<RecipeIngredientDto> ingredients = new();

    [ObservableProperty]
    private ObservableCollection<RecipeDto> recipes = new();

    [ObservableProperty]
    private ObservableCollection<IngredientDto> availableIngredients = new();

    [ObservableProperty]
    private ObservableCollection<RecipeVersionDto> recipeVersions = new();

    [ObservableProperty]
    private string recipeName = string.Empty;

    [ObservableProperty]
    private string recipeCode = string.Empty;

    [ObservableProperty]
    private decimal yieldPerBatch;

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

    public RecipeEditorViewModel(
        IRecipeService recipeService,
        IIngredientService ingredientService,
        IValidator<Recipe> recipeValidator,
        ILogger<RecipeEditorViewModel> logger) : base(logger)
    {
        _recipeService = recipeService;
        _ingredientService = ingredientService;
        _recipeValidator = recipeValidator;
    }

    [RelayCommand]
    public async Task LoadRecipesAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var recipes = await _recipeService.GetActiveRecipesAsync();
            Recipes = new ObservableCollection<RecipeDto>(recipes);
            await LoadAvailableIngredientsAsync();

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
    public async Task CreateRecipeAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(RecipeName) || string.IsNullOrWhiteSpace(RecipeCode))
            {
                SetError("Recipe name and code are required");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new CreateRecipeDto
            {
                Name = RecipeName,
                Code = RecipeCode,
                YieldPerBatch = YieldPerBatch,
                YieldUnitId = 1 // Default to pcs
            };

            CurrentRecipe = await _recipeService.CreateRecipeAsync(dto);
            Ingredients.Clear();

            RecipeName = string.Empty;
            RecipeCode = string.Empty;
            YieldPerBatch = 0;

            await LoadRecipesAsync();
            LogInfo($"Recipe '{CurrentRecipe?.Name}' created successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error creating recipe: {ex.Message}");
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

            SelectedIngredient = null;
            SelectedIngredientQty = 0;

            LogInfo($"Ingredient '{SelectedIngredient?.Name}' added to recipe");
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
    public async Task CreateVersionAsync()
    {
        try
        {
            if (CurrentRecipe == null)
                return;

            IsLoading = true;
            ClearError();

            var dto = new CreateRecipeVersionDto
            {
                ChangeNotes = "Version created",
                EffectiveDate = DateTime.UtcNow,
                YieldPerBatch = CurrentRecipe.YieldPerBatch,
                YieldUnitId = CurrentRecipe.YieldUnitId
            };

            var newVersion = await _recipeService.CreateRecipeVersionAsync(CurrentRecipe.Id, dto);
            await LoadRecipeVersionsAsync(CurrentRecipe.Id);

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

    private async Task LoadAvailableIngredientsAsync()
    {
        var ingredients = await _ingredientService.GetAllIngredientsAsync();
        AvailableIngredients = new ObservableCollection<IngredientDto>(ingredients);
    }

    private async Task LoadRecipeVersionsAsync(int recipeId)
    {
        var versions = await _recipeService.GetRecipeVersionHistoryAsync(recipeId);
        RecipeVersions = new ObservableCollection<RecipeVersionDto>(versions);
    }
}
