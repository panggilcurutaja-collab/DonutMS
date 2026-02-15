using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
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
    Task<IEnumerable<IngredientPriceDto>> GetIngredientPricesAsync(int ingredientId);
    Task<IngredientPriceDto> CreateIngredientPriceAsync(CreateIngredientPriceDto dto);
    Task<IngredientPriceDto> UpdateIngredientPriceAsync(int priceId, UpdateIngredientPriceDto dto);
    Task<bool> DeleteIngredientPriceAsync(int priceId);
}

public class IngredientService : IIngredientService
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IRepository<IngredientPrice> _ingredientPriceRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Unit> _unitRepository;
    private readonly ILogger<IngredientService> _logger;
    private readonly IMapper _mapper;
    private readonly IValidator<Ingredient> _validator;
    private readonly IValidator<IngredientPrice> _priceValidator;

    public IngredientService(
        IIngredientRepository ingredientRepository,
        IRepository<IngredientPrice> ingredientPriceRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<Unit> unitRepository,
        ILogger<IngredientService> logger,
        IMapper mapper,
        IValidator<Ingredient> validator,
        IValidator<IngredientPrice> priceValidator)
    {
        _ingredientRepository = ingredientRepository;
        _ingredientPriceRepository = ingredientPriceRepository;
        _supplierRepository = supplierRepository;
        _unitRepository = unitRepository;
        _logger = logger;
        _mapper = mapper;
        _validator = validator;
        _priceValidator = priceValidator;
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

        if (dto.Notes != null)
            ingredient.Notes = dto.Notes;

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

    public async Task<IEnumerable<IngredientPriceDto>> GetIngredientPricesAsync(int ingredientId)
    {
        var prices = await _ingredientPriceRepository.AsQueryable()
            .Include(p => p.Supplier)
            .Include(p => p.Unit)
            .Where(p => p.IngredientId == ingredientId)
            .OrderByDescending(p => p.EffectiveDate)
            .ToListAsync();

        return _mapper.Map<IEnumerable<IngredientPriceDto>>(prices);
    }

    public async Task<IngredientPriceDto> CreateIngredientPriceAsync(CreateIngredientPriceDto dto)
    {
        var ingredient = await _ingredientRepository.GetByIdAsync(dto.IngredientId);
        if (ingredient == null)
            throw new KeyNotFoundException($"Ingredient {dto.IngredientId} not found");

        var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId);
        if (supplier == null)
            throw new KeyNotFoundException($"Supplier {dto.SupplierId} not found");

        var unit = await _unitRepository.GetByIdAsync(dto.UnitId);
        if (unit == null)
            throw new KeyNotFoundException($"Unit {dto.UnitId} not found");

        if (dto.EffectiveDate == default)
            dto.EffectiveDate = DateTime.UtcNow;

        if (dto.EndDate.HasValue && dto.EndDate.Value <= dto.EffectiveDate)
            throw new InvalidOperationException("End date must be after effective date");

        if (dto.IsActive)
        {
            var activePrices = await _ingredientPriceRepository.AsQueryable()
                .IgnoreQueryFilters()
                .Where(p => p.IngredientId == dto.IngredientId && p.SupplierId == dto.SupplierId && p.IsActive && !p.IsDeleted)
                .ToListAsync();

            foreach (var price in activePrices)
            {
                price.IsActive = false;
                price.EndDate = dto.EffectiveDate;
                await _ingredientPriceRepository.UpdateAsync(price);
            }
        }

        var newPrice = _mapper.Map<IngredientPrice>(dto);
        var validation = await _priceValidator.ValidateAsync(newPrice);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(x => x.ErrorMessage));
            throw new ValidationException($"Ingredient price validation failed: {errors}");
        }

        await _ingredientPriceRepository.AddAsync(newPrice);
        await _ingredientPriceRepository.SaveChangesAsync();

        _logger.LogInformation($"Price added for ingredient '{ingredient.Name}' from supplier '{supplier.Name}'");
        return _mapper.Map<IngredientPriceDto>(newPrice);
    }

    public async Task<IngredientPriceDto> UpdateIngredientPriceAsync(int priceId, UpdateIngredientPriceDto dto)
    {
        var price = await _ingredientPriceRepository.GetByIdAsync(priceId);
        if (price == null)
            throw new KeyNotFoundException($"Ingredient price {priceId} not found");

        if (dto.Price.HasValue)
            price.Price = dto.Price.Value;

        if (dto.UnitId.HasValue)
        {
            var unit = await _unitRepository.GetByIdAsync(dto.UnitId.Value);
            if (unit == null)
                throw new KeyNotFoundException($"Unit {dto.UnitId.Value} not found");
            price.UnitId = dto.UnitId.Value;
        }

        if (dto.EffectiveDate.HasValue)
            price.EffectiveDate = dto.EffectiveDate.Value;

        if (dto.EndDate.HasValue)
            price.EndDate = dto.EndDate.Value;

        if (dto.MinimumQuantity.HasValue)
            price.MinimumQuantity = dto.MinimumQuantity.Value;

        if (dto.Notes != null)
            price.Notes = dto.Notes;

        if (dto.IsActive.HasValue)
            price.IsActive = dto.IsActive.Value;

        if (price.EndDate.HasValue && price.EndDate.Value <= price.EffectiveDate)
            throw new InvalidOperationException("End date must be after effective date");

        if (price.IsActive)
        {
            var activePrices = await _ingredientPriceRepository.AsQueryable()
                .IgnoreQueryFilters()
                .Where(p => p.Id != price.Id && p.IngredientId == price.IngredientId && p.SupplierId == price.SupplierId && p.IsActive && !p.IsDeleted)
                .ToListAsync();

            foreach (var active in activePrices)
            {
                active.IsActive = false;
                active.EndDate = price.EffectiveDate;
                await _ingredientPriceRepository.UpdateAsync(active);
            }
        }

        var validation = await _priceValidator.ValidateAsync(price);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(x => x.ErrorMessage));
            throw new ValidationException($"Ingredient price validation failed: {errors}");
        }

        await _ingredientPriceRepository.UpdateAsync(price);
        await _ingredientPriceRepository.SaveChangesAsync();

        _logger.LogInformation($"Ingredient price {price.Id} updated");
        return _mapper.Map<IngredientPriceDto>(price);
    }

    public async Task<bool> DeleteIngredientPriceAsync(int priceId)
    {
        var price = await _ingredientPriceRepository.GetByIdAsync(priceId);
        if (price == null)
            return false;

        price.IsDeleted = true;
        price.IsActive = false;
        await _ingredientPriceRepository.UpdateAsync(price);
        await _ingredientPriceRepository.SaveChangesAsync();

        _logger.LogInformation($"Ingredient price {price.Id} deleted");
        return true;
    }
}
