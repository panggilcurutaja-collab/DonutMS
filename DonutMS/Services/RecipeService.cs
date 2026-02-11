using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface IRecipeService
{
    Task<RecipeDto?> GetRecipeByIdAsync(int id);
    Task<IEnumerable<RecipeDto>> GetAllRecipesAsync();
    Task<IEnumerable<RecipeDto>> GetActiveRecipesAsync();
    Task<RecipeDto?> GetRecipeByCodeAsync(string code);
    Task<RecipeDto> CreateRecipeAsync(CreateRecipeDto dto);
    Task<RecipeDto> UpdateRecipeAsync(int id, UpdateRecipeDto dto);
    Task<bool> DeleteRecipeAsync(int id);
    
    Task<RecipeVersionDto?> GetRecipeVersionAsync(int recipeId, int versionNumber);
    Task<IEnumerable<RecipeVersionDto>> GetRecipeVersionHistoryAsync(int recipeId);
    Task<RecipeVersionDto> CreateRecipeVersionAsync(int recipeId, CreateRecipeVersionDto dto);
    Task<bool> RollbackToVersionAsync(int recipeId, int versionNumber);
    
    Task<IEnumerable<RecipeIngredientDto>> GetRecipeIngredientsAsync(int recipeId);
    Task<bool> AddIngredientToRecipeAsync(int recipeId, AddRecipeIngredientDto dto);
    Task<bool> RemoveIngredientFromRecipeAsync(int recipeId, int ingredientId);
    Task<bool> UpdateRecipeIngredientAsync(int recipeId, int ingredientId, UpdateRecipeIngredientDto dto);
    
    Task<RecipeSubstitutionDto?> GetSubstitutionAsync(int originalIngredientId, int substituteIngredientId);
    Task<IEnumerable<RecipeSubstitutionDto>> GetSubstitutionsForIngredientAsync(int ingredientId);
    Task<IEnumerable<RecipeSubstitutionDto>> GetAllSubstitutionsAsync();
    Task<RecipeSubstitutionDto> CreateSubstitutionAsync(CreateRecipeSubstitutionDto dto);
    Task<bool> ApproveSubstitutionAsync(int originalIngredientId, int substituteIngredientId);
}

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IRepository<RecipeSubstitution> _substitutionRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<RecipeService> _logger;
    private readonly IMapper _mapper;
    private readonly IValidator<Recipe> _recipeValidator;

    public RecipeService(
        IRecipeRepository recipeRepository,
        IIngredientRepository ingredientRepository,
        IRepository<RecipeSubstitution> substitutionRepository,
        IAuditService auditService,
        ILogger<RecipeService> logger,
        IMapper mapper,
        IValidator<Recipe> recipeValidator)
    {
        _recipeRepository = recipeRepository;
        _ingredientRepository = ingredientRepository;
        _substitutionRepository = substitutionRepository;
        _auditService = auditService;
        _logger = logger;
        _mapper = mapper;
        _recipeValidator = recipeValidator;
    }

    public async Task<RecipeDto?> GetRecipeByIdAsync(int id)
    {
        var recipe = await _recipeRepository.GetRecipeWithIngredientsAsync(id);
        return recipe != null ? _mapper.Map<RecipeDto>(recipe) : null;
    }

    public async Task<IEnumerable<RecipeDto>> GetAllRecipesAsync()
    {
        var recipes = await _recipeRepository
            .AsQueryable()
            .Include(r => r.YieldUnit)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync();
        return _mapper.Map<IEnumerable<RecipeDto>>(recipes);
    }

    public async Task<IEnumerable<RecipeDto>> GetActiveRecipesAsync()
    {
        var recipes = await _recipeRepository.GetActiveRecipesAsync();
        return _mapper.Map<IEnumerable<RecipeDto>>(recipes);
    }

    public async Task<RecipeDto?> GetRecipeByCodeAsync(string code)
    {
        var recipe = await _recipeRepository.GetByCodeAsync(code);
        return recipe != null ? _mapper.Map<RecipeDto>(recipe) : null;
    }

    public async Task<RecipeDto> CreateRecipeAsync(CreateRecipeDto dto)
    {
        try
        {
            var recipe = _mapper.Map<Recipe>(dto);
            
            var validationResult = await _recipeValidator.ValidateAsync(recipe);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage));
                throw new ValidationException($"Recipe validation failed: {errors}");
            }

            await _recipeRepository.AddAsync(recipe);
            await _recipeRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                "Recipe",
                recipe.Id,
                "Create",
                remarks: recipe.Name,
                newValues: JsonConvert.SerializeObject(new { recipe.Name, recipe.Code, recipe.YieldPerBatch, recipe.YieldUnitId }));

            _logger.LogInformation($"Recipe '{recipe.Name}' (Code: {recipe.Code}) created successfully");
            return _mapper.Map<RecipeDto>(recipe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recipe");
            throw;
        }
    }

    public async Task<RecipeDto> UpdateRecipeAsync(int id, UpdateRecipeDto dto)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id);
        if (recipe == null)
            throw new KeyNotFoundException($"Recipe with ID {id} not found");

        var oldSnapshot = JsonConvert.SerializeObject(new
        {
            recipe.Name,
            recipe.Code,
            recipe.Description,
            recipe.YieldPerBatch,
            recipe.YieldUnitId,
            recipe.IsActive
        });

        _mapper.Map(dto, recipe);
        
        var validationResult = await _recipeValidator.ValidateAsync(recipe);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage));
            throw new ValidationException($"Recipe validation failed: {errors}");
        }

        await _recipeRepository.UpdateAsync(recipe);
        await _recipeRepository.SaveChangesAsync();

        var newSnapshot = JsonConvert.SerializeObject(new
        {
            recipe.Name,
            recipe.Code,
            recipe.Description,
            recipe.YieldPerBatch,
            recipe.YieldUnitId,
            recipe.IsActive
        });

        await _auditService.LogAsync(
            "Recipe",
            recipe.Id,
            "Update",
            oldValues: oldSnapshot,
            newValues: newSnapshot,
            remarks: recipe.Name);

        _logger.LogInformation($"Recipe '{recipe.Name}' updated successfully");
        return _mapper.Map<RecipeDto>(recipe);
    }

    public async Task<bool> DeleteRecipeAsync(int id)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id);
        if (recipe == null)
            return false;

        recipe.IsDeleted = true;
        await _recipeRepository.UpdateAsync(recipe);
        await _recipeRepository.SaveChangesAsync();

        await _auditService.LogAsync("Recipe", recipe.Id, "Delete", remarks: recipe.Name);

        _logger.LogInformation($"Recipe '{recipe.Name}' deleted");
        return true;
    }

    public async Task<RecipeVersionDto?> GetRecipeVersionAsync(int recipeId, int versionNumber)
    {
        var version = await _recipeRepository
            .AsQueryable()
            .Where(r => r.Id == recipeId)
            .SelectMany(r => r.Versions!)
            .FirstOrDefaultAsync(v => v.VersionNumber == versionNumber && !v.IsDeleted);

        return version != null ? _mapper.Map<RecipeVersionDto>(version) : null;
    }

    public async Task<IEnumerable<RecipeVersionDto>> GetRecipeVersionHistoryAsync(int recipeId)
    {
        var versions = await _recipeRepository
            .AsQueryable()
            .Where(r => r.Id == recipeId)
            .SelectMany(r => r.Versions!)
            .Where(v => !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();

        return _mapper.Map<IEnumerable<RecipeVersionDto>>(versions);
    }

    public async Task<RecipeVersionDto> CreateRecipeVersionAsync(int recipeId, CreateRecipeVersionDto dto)
    {
        var recipe = await _recipeRepository.GetRecipeWithIngredientsAsync(recipeId);
        if (recipe == null)
            throw new KeyNotFoundException($"Recipe with ID {recipeId} not found");

        var latestVersionNumber = await _recipeRepository
            .AsQueryable()
            .Where(r => r.Id == recipeId)
            .SelectMany(r => r.Versions!)
            .Where(v => !v.IsDeleted)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync() ?? 0;
        var newVersionNumber = latestVersionNumber + 1;

        var version = new RecipeVersion
        {
            RecipeId = recipeId,
            VersionNumber = newVersionNumber,
            ChangeNotes = dto.ChangeNotes,
            EffectiveDate = dto.EffectiveDate,
            YieldPerBatch = dto.YieldPerBatch,
            YieldUnitId = dto.YieldUnitId
        };

        if (recipe.RecipeIngredients != null && recipe.RecipeIngredients.Count > 0)
        {
            version.Ingredients = recipe.RecipeIngredients
                .OrderBy(i => i.SortOrder)
                .Select(i => new RecipeVersionIngredient
                {
                    IngredientId = i.IngredientId,
                    QuantityPerBatch = i.QuantityPerBatch,
                    UnitId = i.UnitId,
                    SortOrder = i.SortOrder,
                    IsOptional = i.IsOptional,
                    WastePercentage = 0,
                    Notes = i.Notes
                }).ToList();
        }

        if (recipe.Versions == null)
            recipe.Versions = new List<RecipeVersion>();
        recipe.Versions.Add(version);

        await _recipeRepository.UpdateAsync(recipe);
        await _recipeRepository.SaveChangesAsync();

        recipe.CurrentVersionId = version.Id;
        await _recipeRepository.UpdateAsync(recipe);
        await _recipeRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            "Recipe",
            recipe.Id,
            "CreateVersion",
            remarks: $"Version {newVersionNumber}");

        _logger.LogInformation($"Recipe version {newVersionNumber} created for recipe ID {recipeId}");
        return _mapper.Map<RecipeVersionDto>(version);
    }

    public async Task<bool> RollbackToVersionAsync(int recipeId, int versionNumber)
    {
        var recipe = await _recipeRepository
            .AsQueryable()
            .Include(r => r.RecipeIngredients)
            .Include(r => r.Versions)
            .ThenInclude(v => v.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == recipeId && !r.IsDeleted);
        if (recipe == null)
            return false;

        var targetVersion = recipe.Versions?.FirstOrDefault(v => v.VersionNumber == versionNumber && !v.IsDeleted);
        if (targetVersion == null)
            return false;

        recipe.CurrentVersionId = targetVersion.Id;
        recipe.YieldPerBatch = targetVersion.YieldPerBatch;
        recipe.YieldUnitId = targetVersion.YieldUnitId;

        if (recipe.RecipeIngredients == null)
            recipe.RecipeIngredients = new List<RecipeIngredient>();
        else
            recipe.RecipeIngredients.Clear();

        foreach (var versionIngredient in targetVersion.Ingredients.OrderBy(i => i.SortOrder))
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.Id,
                IngredientId = versionIngredient.IngredientId,
                QuantityPerBatch = versionIngredient.QuantityPerBatch,
                UnitId = versionIngredient.UnitId,
                SortOrder = versionIngredient.SortOrder,
                IsOptional = versionIngredient.IsOptional,
                Notes = versionIngredient.Notes
            });
        }

        await _recipeRepository.UpdateAsync(recipe);
        await _recipeRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            "Recipe",
            recipe.Id,
            "Rollback",
            remarks: $"Version {versionNumber}");

        _logger.LogInformation($"Recipe '{recipe.Name}' rolled back to version {versionNumber}");
        return true;
    }

    public async Task<IEnumerable<RecipeIngredientDto>> GetRecipeIngredientsAsync(int recipeId)
    {
        var ingredients = await _recipeRepository
            .AsQueryable()
            .Where(r => r.Id == recipeId)
            .SelectMany(r => r.RecipeIngredients!)
            .ToListAsync();

        return _mapper.Map<IEnumerable<RecipeIngredientDto>>(ingredients);
    }

    public async Task<bool> AddIngredientToRecipeAsync(int recipeId, AddRecipeIngredientDto dto)
    {
        var recipe = await _recipeRepository.GetRecipeWithIngredientsAsync(recipeId);
        if (recipe == null)
            return false;

        var ingredient = await _ingredientRepository.GetByIdAsync(dto.IngredientId);
        if (ingredient == null)
            throw new KeyNotFoundException($"Ingredient with ID {dto.IngredientId} not found");

        if (recipe.RecipeIngredients != null && recipe.RecipeIngredients.Any(ri => ri.IngredientId == dto.IngredientId))
            throw new InvalidOperationException("Ingredient already exists in this recipe");

        var recipeIngredient = new RecipeIngredient
        {
            RecipeId = recipeId,
            IngredientId = dto.IngredientId,
            QuantityPerBatch = dto.QuantityPerBatch,
            UnitId = dto.UnitId,
            SortOrder = recipe.RecipeIngredients?.Count ?? 0
        };

        if (recipe.RecipeIngredients == null)
            recipe.RecipeIngredients = new List<RecipeIngredient>();
        recipe.RecipeIngredients.Add(recipeIngredient);

        await _recipeRepository.UpdateAsync(recipe);
        await _recipeRepository.SaveChangesAsync();

        _logger.LogInformation($"Ingredient '{ingredient.Name}' added to recipe '{recipe.Name}'");
        return true;
    }

    public async Task<bool> RemoveIngredientFromRecipeAsync(int recipeId, int ingredientId)
    {
        var recipe = await _recipeRepository.GetRecipeWithIngredientsAsync(recipeId);
        if (recipe == null)
            return false;

        var ingredient = recipe.RecipeIngredients?.FirstOrDefault(ri => ri.IngredientId == ingredientId);
        if (ingredient == null)
            return false;

        recipe.RecipeIngredients!.Remove(ingredient);
        await _recipeRepository.UpdateAsync(recipe);
        await _recipeRepository.SaveChangesAsync();

        _logger.LogInformation($"Ingredient removed from recipe '{recipe.Name}'");
        return true;
    }

    public async Task<bool> UpdateRecipeIngredientAsync(int recipeId, int ingredientId, UpdateRecipeIngredientDto dto)
    {
        var recipe = await _recipeRepository.GetRecipeWithIngredientsAsync(recipeId);
        if (recipe == null)
            return false;

        var ingredient = recipe.RecipeIngredients?.FirstOrDefault(ri => ri.IngredientId == ingredientId);
        if (ingredient == null)
            return false;

        ingredient.QuantityPerBatch = dto.QuantityPerBatch;
        ingredient.UnitId = dto.UnitId;

        await _recipeRepository.UpdateAsync(recipe);
        await _recipeRepository.SaveChangesAsync();

        _logger.LogInformation($"Recipe ingredient updated in recipe '{recipe.Name}'");
        return true;
    }

    public async Task<RecipeSubstitutionDto?> GetSubstitutionAsync(int originalIngredientId, int substituteIngredientId)
    {
        var substitution = await _substitutionRepository
            .AsQueryable()
            .Include(s => s.OriginalIngredient)
            .Include(s => s.SubstituteIngredient)
            .FirstOrDefaultAsync(s =>
                !s.IsDeleted &&
                s.OriginalIngredientId == originalIngredientId &&
                s.SubstituteIngredientId == substituteIngredientId);

        return substitution != null ? _mapper.Map<RecipeSubstitutionDto>(substitution) : null;
    }

    public async Task<IEnumerable<RecipeSubstitutionDto>> GetSubstitutionsForIngredientAsync(int ingredientId)
    {
        var substitutions = await _substitutionRepository
            .AsQueryable()
            .Include(s => s.OriginalIngredient)
            .Include(s => s.SubstituteIngredient)
            .Where(s => !s.IsDeleted && s.OriginalIngredientId == ingredientId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<RecipeSubstitutionDto>>(substitutions);
    }

    public async Task<IEnumerable<RecipeSubstitutionDto>> GetAllSubstitutionsAsync()
    {
        var substitutions = await _substitutionRepository
            .AsQueryable()
            .Include(s => s.OriginalIngredient)
            .Include(s => s.SubstituteIngredient)
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<RecipeSubstitutionDto>>(substitutions);
    }

    public async Task<RecipeSubstitutionDto> CreateSubstitutionAsync(CreateRecipeSubstitutionDto dto)
    {
        if (dto.OriginalIngredientId == dto.SubstituteIngredientId)
            throw new InvalidOperationException("Original and substitute ingredients must be different");

        if (dto.SubstitutionRatio <= 0)
            throw new InvalidOperationException("Substitution ratio must be greater than zero");

        var original = await _ingredientRepository.GetByIdAsync(dto.OriginalIngredientId);
        var substitute = await _ingredientRepository.GetByIdAsync(dto.SubstituteIngredientId);

        if (original == null || substitute == null)
            throw new KeyNotFoundException("One or both ingredients not found");

        var existing = await _substitutionRepository
            .AsQueryable()
            .FirstOrDefaultAsync(s =>
                !s.IsDeleted &&
                s.OriginalIngredientId == dto.OriginalIngredientId &&
                s.SubstituteIngredientId == dto.SubstituteIngredientId);

        if (existing != null)
            throw new InvalidOperationException("Substitution rule already exists");

        var substitution = _mapper.Map<RecipeSubstitution>(dto);
        substitution.IsApproved = false;

        await _substitutionRepository.AddAsync(substitution);
        await _substitutionRepository.SaveChangesAsync();

        _logger.LogInformation($"Substitution created: {original.Name} -> {substitute.Name}");
        return _mapper.Map<RecipeSubstitutionDto>(substitution);
    }

    public async Task<bool> ApproveSubstitutionAsync(int originalIngredientId, int substituteIngredientId)
    {
        var substitution = await _substitutionRepository
            .AsQueryable()
            .FirstOrDefaultAsync(s =>
                !s.IsDeleted &&
                s.OriginalIngredientId == originalIngredientId &&
                s.SubstituteIngredientId == substituteIngredientId);

        if (substitution == null)
            return false;

        substitution.IsApproved = true;
        await _substitutionRepository.UpdateAsync(substitution);
        await _substitutionRepository.SaveChangesAsync();

        _logger.LogInformation($"Substitution approved: {originalIngredientId} -> {substituteIngredientId}");
        return true;
    }
}
