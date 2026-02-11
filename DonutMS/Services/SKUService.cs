using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface ISKUService
{
    Task<IEnumerable<SKUDto>> GetAllSkusAsync();
    Task<SKUDto?> GetSkuByIdAsync(int id);
    Task<SKUDto?> GetSkuByCodeAsync(string code);
    Task<SKUDto> CreateSkuAsync(CreateSKUDto dto);
    Task<SKUDto> UpdateSkuAsync(int id, UpdateSKUDto dto);
    Task<bool> DeleteSkuAsync(int id);

    Task<IEnumerable<SKUCostDto>> GetSkuCostHistoryAsync(int skuId);
    Task<SKUCostDto> AddSkuCostVersionAsync(int skuId, CreateSKUCostDto dto);

    Task<IEnumerable<AllergenDto>> GetAllergensAsync();
    Task<IEnumerable<SKUAllergenDto>> GetSkuAllergensAsync(int skuId);
    Task SaveSkuAllergensAsync(int skuId, IEnumerable<SKUAllergenDto> allergens);

    Task<NutritionalInfoDto?> GetNutritionalInfoAsync(int skuId);
    Task<NutritionalInfoDto> UpsertNutritionalInfoAsync(int skuId, UpsertNutritionalInfoDto dto);
}

public class SKUService : ISKUService
{
    private readonly ISKURepository _skuRepository;
    private readonly IRepository<SKUCost> _skuCostRepository;
    private readonly IRepository<SKUAllergen> _skuAllergenRepository;
    private readonly IRepository<Allergen> _allergenRepository;
    private readonly IRepository<NutritionalInfo> _nutritionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SKUService> _logger;
    private readonly IValidator<SKU> _validator;
    private readonly IAuditService _auditService;

    public SKUService(
        ISKURepository skuRepository,
        IRepository<SKUCost> skuCostRepository,
        IRepository<SKUAllergen> skuAllergenRepository,
        IRepository<Allergen> allergenRepository,
        IRepository<NutritionalInfo> nutritionRepository,
        IMapper mapper,
        ILogger<SKUService> logger,
        IValidator<SKU> validator,
        IAuditService auditService)
    {
        _skuRepository = skuRepository;
        _skuCostRepository = skuCostRepository;
        _skuAllergenRepository = skuAllergenRepository;
        _allergenRepository = allergenRepository;
        _nutritionRepository = nutritionRepository;
        _mapper = mapper;
        _logger = logger;
        _validator = validator;
        _auditService = auditService;
    }

    public async Task<IEnumerable<SKUDto>> GetAllSkusAsync()
    {
        var skus = await _skuRepository.AsQueryable()
            .Include(s => s.Recipe)
            .Include(s => s.Costs)
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return skus.Select(MapSkuWithCost).ToList();
    }

    public async Task<SKUDto?> GetSkuByIdAsync(int id)
    {
        var sku = await _skuRepository.AsQueryable()
            .Include(s => s.Recipe)
            .Include(s => s.Costs)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        return sku == null ? null : MapSkuWithCost(sku);
    }

    public async Task<SKUDto?> GetSkuByCodeAsync(string code)
    {
        var sku = await _skuRepository.AsQueryable()
            .Include(s => s.Recipe)
            .Include(s => s.Costs)
            .FirstOrDefaultAsync(s => s.Code == code && !s.IsDeleted);

        return sku == null ? null : MapSkuWithCost(sku);
    }

    public async Task<SKUDto> CreateSkuAsync(CreateSKUDto dto)
    {
        var existing = await _skuRepository.GetByCodeAsync(dto.Code);
        if (existing != null)
            throw new InvalidOperationException($"SKU code '{dto.Code}' already exists");

        var sku = _mapper.Map<SKU>(dto);
        var validationResult = await _validator.ValidateAsync(sku);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage));
            throw new ValidationException($"SKU validation failed: {errors}");
        }

        await _skuRepository.AddAsync(sku);
        await _skuRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            "SKU",
            sku.Id,
            "Create",
            remarks: sku.Code,
            newValues: JsonConvert.SerializeObject(new { sku.Name, sku.Code, sku.RetailPrice, sku.RecipeId }));

        _logger.LogInformation($"SKU '{sku.Name}' created");
        return MapSkuWithCost(sku);
    }

    public async Task<SKUDto> UpdateSkuAsync(int id, UpdateSKUDto dto)
    {
        var sku = await _skuRepository.GetByIdAsync(id);
        if (sku == null)
            throw new KeyNotFoundException($"SKU with ID {id} not found");

        var oldSnapshot = JsonConvert.SerializeObject(new
        {
            sku.Name,
            sku.Code,
            sku.Description,
            sku.Category,
            sku.RecipeId,
            sku.RetailPrice,
            sku.IsActive
        });

        if (!string.IsNullOrWhiteSpace(dto.Code) && !string.Equals(dto.Code, sku.Code, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _skuRepository.GetByCodeAsync(dto.Code);
            if (existing != null && existing.Id != id)
                throw new InvalidOperationException($"SKU code '{dto.Code}' already exists");
            sku.Code = dto.Code;
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
            sku.Name = dto.Name;

        if (dto.Description != null)
            sku.Description = dto.Description;

        if (dto.Category != null)
            sku.Category = dto.Category;

        sku.RecipeId = dto.RecipeId;

        if (dto.RetailPrice.HasValue)
            sku.RetailPrice = dto.RetailPrice.Value;

        if (dto.IsActive.HasValue)
            sku.IsActive = dto.IsActive.Value;

        var validationResult = await _validator.ValidateAsync(sku);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage));
            throw new ValidationException($"SKU validation failed: {errors}");
        }

        await _skuRepository.UpdateAsync(sku);
        await _skuRepository.SaveChangesAsync();

        var newSnapshot = JsonConvert.SerializeObject(new
        {
            sku.Name,
            sku.Code,
            sku.Description,
            sku.Category,
            sku.RecipeId,
            sku.RetailPrice,
            sku.IsActive
        });

        await _auditService.LogAsync(
            "SKU",
            sku.Id,
            "Update",
            oldValues: oldSnapshot,
            newValues: newSnapshot,
            remarks: sku.Code);

        _logger.LogInformation($"SKU '{sku.Name}' updated");
        return await GetSkuByIdAsync(id) ?? _mapper.Map<SKUDto>(sku);
    }

    public async Task<bool> DeleteSkuAsync(int id)
    {
        var sku = await _skuRepository.GetByIdAsync(id);
        if (sku == null)
            return false;

        sku.IsDeleted = true;
        await _skuRepository.UpdateAsync(sku);
        await _skuRepository.SaveChangesAsync();

        await _auditService.LogAsync("SKU", sku.Id, "Delete", remarks: sku.Code);

        _logger.LogInformation($"SKU '{sku.Name}' deleted");
        return true;
    }

    public async Task<IEnumerable<SKUCostDto>> GetSkuCostHistoryAsync(int skuId)
    {
        var costs = await _skuCostRepository.AsQueryable()
            .Where(c => c.SKUId == skuId)
            .OrderByDescending(c => c.EffectiveDate)
            .ToListAsync();

        return _mapper.Map<IEnumerable<SKUCostDto>>(costs);
    }

    public async Task<SKUCostDto> AddSkuCostVersionAsync(int skuId, CreateSKUCostDto dto)
    {
        var sku = await _skuRepository.GetByIdAsync(skuId);
        if (sku == null)
            throw new KeyNotFoundException($"SKU with ID {skuId} not found");

        var oldCost = await _skuCostRepository.AsQueryable()
            .Where(c => c.SKUId == skuId && c.IsActive)
            .OrderByDescending(c => c.EffectiveDate)
            .FirstOrDefaultAsync();

        var now = DateTime.UtcNow;
        var existingActive = await _skuCostRepository.AsQueryable()
            .Where(c => c.SKUId == skuId && c.IsActive)
            .ToListAsync();

        foreach (var cost in existingActive)
        {
            cost.IsActive = false;
            cost.EndDate = now;
            await _skuCostRepository.UpdateAsync(cost);
        }

        var totalHpp = dto.MaterialCost + dto.PackagingCost + dto.LaborCost + dto.OverheadCost;
        var grossMargin = sku.RetailPrice > 0 ? ((sku.RetailPrice - totalHpp) / sku.RetailPrice) * 100m : 0m;

        var newCost = new SKUCost
        {
            SKUId = skuId,
            MaterialCost = dto.MaterialCost,
            PackagingCost = dto.PackagingCost,
            LaborCost = dto.LaborCost,
            OverheadCost = dto.OverheadCost,
            TotalHPP = totalHpp,
            GrossMargin = grossMargin,
            EffectiveDate = now,
            IsActive = true
        };

        await _skuCostRepository.AddAsync(newCost);
        await _skuCostRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            "SKUCost",
            newCost.Id,
            "AddCostVersion",
            oldValues: oldCost == null ? null : JsonConvert.SerializeObject(new
            {
                oldCost.MaterialCost,
                oldCost.PackagingCost,
                oldCost.LaborCost,
                oldCost.OverheadCost,
                oldCost.TotalHPP
            }),
            newValues: JsonConvert.SerializeObject(new
            {
                newCost.MaterialCost,
                newCost.PackagingCost,
                newCost.LaborCost,
                newCost.OverheadCost,
                newCost.TotalHPP
            }),
            remarks: sku.Code);

        _logger.LogInformation($"SKU cost version created for SKU '{sku.Code}'");
        return _mapper.Map<SKUCostDto>(newCost);
    }

    public async Task<IEnumerable<AllergenDto>> GetAllergensAsync()
    {
        var allergens = await _allergenRepository.AsQueryable()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<AllergenDto>>(allergens);
    }

    public async Task<IEnumerable<SKUAllergenDto>> GetSkuAllergensAsync(int skuId)
    {
        var skuAllergens = await _skuAllergenRepository.AsQueryable()
            .Include(sa => sa.Allergen)
            .Where(sa => sa.SKUId == skuId)
            .OrderBy(sa => sa.Allergen!.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<SKUAllergenDto>>(skuAllergens);
    }

    public async Task SaveSkuAllergensAsync(int skuId, IEnumerable<SKUAllergenDto> allergens)
    {
        var existing = await _skuAllergenRepository.AsQueryable()
            .Where(sa => sa.SKUId == skuId)
            .ToListAsync();

        var incoming = allergens.ToList();
        var incomingIds = incoming.Select(a => a.AllergenId).ToHashSet();

        foreach (var existingItem in existing)
        {
            var updated = incoming.FirstOrDefault(a => a.AllergenId == existingItem.AllergenId);
            if (updated == null)
            {
                await _skuAllergenRepository.DeleteAsync(existingItem);
            }
            else
            {
                existingItem.MayContainTrace = updated.MayContainTrace;
                existingItem.Notes = updated.Notes;
                await _skuAllergenRepository.UpdateAsync(existingItem);
            }
        }

        foreach (var add in incoming.Where(a => existing.All(e => e.AllergenId != a.AllergenId)))
        {
            var entity = new SKUAllergen
            {
                SKUId = skuId,
                AllergenId = add.AllergenId,
                MayContainTrace = add.MayContainTrace,
                Notes = add.Notes
            };
            await _skuAllergenRepository.AddAsync(entity);
        }

        await _skuAllergenRepository.SaveChangesAsync();
        _logger.LogInformation($"SKU allergens updated for SKU {skuId}");
    }

    public async Task<NutritionalInfoDto?> GetNutritionalInfoAsync(int skuId)
    {
        var nutrition = await _nutritionRepository.AsQueryable()
            .Where(n => n.SKUId == skuId)
            .OrderByDescending(n => n.EffectiveDate)
            .FirstOrDefaultAsync();

        return nutrition == null ? null : _mapper.Map<NutritionalInfoDto>(nutrition);
    }

    public async Task<NutritionalInfoDto> UpsertNutritionalInfoAsync(int skuId, UpsertNutritionalInfoDto dto)
    {
        var existing = await _nutritionRepository.AsQueryable()
            .Where(n => n.SKUId == skuId)
            .OrderByDescending(n => n.EffectiveDate)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            var entity = _mapper.Map<NutritionalInfo>(dto);
            entity.SKUId = skuId;
            entity.EffectiveDate = DateTime.UtcNow;

            await _nutritionRepository.AddAsync(entity);
            await _nutritionRepository.SaveChangesAsync();

            _logger.LogInformation($"Nutritional info created for SKU {skuId}");
            return _mapper.Map<NutritionalInfoDto>(entity);
        }

        existing.Calories = dto.Calories;
        existing.Protein = dto.Protein;
        existing.Fat = dto.Fat;
        existing.Carbohydrates = dto.Carbohydrates;
        existing.Fiber = dto.Fiber;
        existing.Sugar = dto.Sugar;
        existing.Sodium = dto.Sodium;
        existing.ServingSize = dto.ServingSize;
        existing.ServingsPerPackage = dto.ServingsPerPackage;
        existing.Notes = dto.Notes;
        existing.EffectiveDate = DateTime.UtcNow;

        await _nutritionRepository.UpdateAsync(existing);
        await _nutritionRepository.SaveChangesAsync();

        _logger.LogInformation($"Nutritional info updated for SKU {skuId}");
        return _mapper.Map<NutritionalInfoDto>(existing);
    }

    private SKUDto MapSkuWithCost(SKU sku)
    {
        var dto = _mapper.Map<SKUDto>(sku);
        var currentCost = GetCurrentCost(sku);

        if (currentCost != null)
        {
            dto.CurrentCost = _mapper.Map<SKUCostDto>(currentCost);
            dto.HPP = currentCost.TotalHPP;
            if (sku.RetailPrice > 0)
            {
                dto.GrossMargin = ((sku.RetailPrice - currentCost.TotalHPP) / sku.RetailPrice) * 100m;
            }
        }

        return dto;
    }

    private static SKUCost? GetCurrentCost(SKU sku)
    {
        if (sku.Costs == null || sku.Costs.Count == 0)
            return null;

        var now = DateTime.UtcNow;
        return sku.Costs
            .Where(c => c.IsActive && c.EffectiveDate <= now && (!c.EndDate.HasValue || c.EndDate >= now))
            .OrderByDescending(c => c.EffectiveDate)
            .FirstOrDefault();
    }
}
