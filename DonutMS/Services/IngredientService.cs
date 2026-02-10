using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface IIngredientService
{
    Task<IngredientDto?> GetIngredientByIdAsync(int id);
    Task<IEnumerable<IngredientDto>> GetAllIngredientsAsync();
    Task<IngredientDto?> GetIngredientBySKUAsync(string sku);
    Task<IngredientDto> CreateIngredientAsync(CreateIngredientDto dto);
    Task<IngredientDto> UpdateIngredientAsync(int id, UpdateIngredientDto dto);
    Task<bool> DeleteIngredientAsync(int id);
    Task<IEnumerable<IngredientDto>> GetLowStockIngredientsAsync();
}

public class IngredientService : IIngredientService
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly ILogger<IngredientService> _logger;
    private readonly IMapper _mapper;
    private readonly IValidator<Ingredient> _validator;

    public IngredientService(
        IIngredientRepository ingredientRepository,
        ILogger<IngredientService> logger,
        IMapper mapper,
        IValidator<Ingredient> validator)
    {
        _ingredientRepository = ingredientRepository;
        _logger = logger;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<IngredientDto?> GetIngredientByIdAsync(int id)
    {
        var ingredient = await _ingredientRepository.GetIngredientWithPricesAsync(id);
        return ingredient != null ? _mapper.Map<IngredientDto>(ingredient) : null;
    }

    public async Task<IEnumerable<IngredientDto>> GetAllIngredientsAsync()
    {
        var ingredients = await _ingredientRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<IngredientDto>>(ingredients);
    }

    public async Task<IngredientDto?> GetIngredientBySKUAsync(string sku)
    {
        var ingredient = await _ingredientRepository.GetBySKUAsync(sku);
        return ingredient != null ? _mapper.Map<IngredientDto>(ingredient) : null;
    }

    public async Task<IngredientDto> CreateIngredientAsync(CreateIngredientDto dto)
    {
        var ingredient = _mapper.Map<Ingredient>(dto);
        
        var validationResult = await _validator.ValidateAsync(ingredient);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage));
            throw new FluentValidation.ValidationException($"Ingredient validation failed: {errors}");
        }

        await _ingredientRepository.AddAsync(ingredient);
        await _ingredientRepository.SaveChangesAsync();

        _logger.LogInformation($"Ingredient '{ingredient.Name}' (SKU: {ingredient.SKU}) created");
        return _mapper.Map<IngredientDto>(ingredient);
    }

    public async Task<IngredientDto> UpdateIngredientAsync(int id, UpdateIngredientDto dto)
    {
        var ingredient = await _ingredientRepository.GetByIdAsync(id);
        if (ingredient == null)
            throw new InvalidOperationException($"Ingredient with ID {id} not found");

        // Update only provided fields
        if (!string.IsNullOrEmpty(dto.Name))
            ingredient.Name = dto.Name;

        if (dto.Description != null)
            ingredient.Description = dto.Description;

        if (dto.ConsumptionUnitId.HasValue)
            ingredient.ConsumptionUnitId = dto.ConsumptionUnitId.Value;

        if (dto.PurchaseUnitId.HasValue)
            ingredient.PurchaseUnitId = dto.PurchaseUnitId.Value;

        if (dto.MinimumStockLevel.HasValue)
            ingredient.MinimumStockLevel = dto.MinimumStockLevel.Value;

        if (dto.ReorderPoint.HasValue)
            ingredient.ReorderPoint = dto.ReorderPoint.Value;

        if (dto.ReorderQuantity.HasValue)
            ingredient.ReorderQuantity = dto.ReorderQuantity.Value;

        if (dto.ShelfLifeDays.HasValue)
            ingredient.ShelfLifeDays = dto.ShelfLifeDays.Value;

        if (dto.IsActive.HasValue)
            ingredient.IsActive = dto.IsActive.Value;

        var validationResult = await _validator.ValidateAsync(ingredient);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage));
            throw new FluentValidation.ValidationException($"Ingredient validation failed: {errors}");
        }

        await _ingredientRepository.UpdateAsync(ingredient);
        await _ingredientRepository.SaveChangesAsync();

        _logger.LogInformation($"Ingredient '{ingredient.Name}' updated");
        return _mapper.Map<IngredientDto>(ingredient);
    }

    public async Task<bool> DeleteIngredientAsync(int id)
    {
        var ingredient = await _ingredientRepository.GetByIdAsync(id);
        if (ingredient == null)
            return false;

        ingredient.IsDeleted = true;
        await _ingredientRepository.UpdateAsync(ingredient);
        await _ingredientRepository.SaveChangesAsync();

        _logger.LogInformation($"Ingredient '{ingredient.Name}' deleted");
        return true;
    }

    public async Task<IEnumerable<IngredientDto>> GetLowStockIngredientsAsync()
    {
        var ingredients = await _ingredientRepository.GetLowStockIngredientsAsync();
        return _mapper.Map<IEnumerable<IngredientDto>>(ingredients);
    }
}
