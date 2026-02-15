using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface IPromoService
{
    Task<IEnumerable<DiscountDto>> GetDiscountsAsync(bool includeInactive = true);
    Task<DiscountDto> SaveDiscountAsync(DiscountDto dto);
    Task<IEnumerable<BundlePackageDto>> GetBundlesAsync(bool includeInactive = true);
    Task<BundlePackageDto> SaveBundleAsync(BundlePackageDto dto);
}

public class PromoService : IPromoService
{
    private readonly IRepository<Discount> _discountRepository;
    private readonly IRepository<BundlePackage> _bundleRepository;
    private readonly ISKURepository _skuRepository;
    private readonly ILogger<PromoService> _logger;
    private readonly IMapper _mapper;

    public PromoService(
        IRepository<Discount> discountRepository,
        IRepository<BundlePackage> bundleRepository,
        ISKURepository skuRepository,
        ILogger<PromoService> logger,
        IMapper mapper)
    {
        _discountRepository = discountRepository;
        _bundleRepository = bundleRepository;
        _skuRepository = skuRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<IEnumerable<DiscountDto>> GetDiscountsAsync(bool includeInactive = true)
    {
        var query = _discountRepository
            .AsQueryable()
            .Where(d => !d.IsDeleted);

        if (!includeInactive)
            query = query.Where(d => d.IsActive);

        var discounts = await query
            .OrderByDescending(d => d.StartDate)
            .ToListAsync();

        return _mapper.Map<IEnumerable<DiscountDto>>(discounts);
    }

    public async Task<DiscountDto> SaveDiscountAsync(DiscountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Discount name is required");

        if (dto.EndDate < dto.StartDate)
            throw new InvalidOperationException("End date must be on or after start date");

        if (dto.DiscountValue < 0)
            throw new InvalidOperationException("Discount value must be 0 or higher");

        if (string.Equals(dto.DiscountType, "Volume", StringComparison.OrdinalIgnoreCase))
        {
            if (!dto.MinimumQuantity.HasValue || dto.MinimumQuantity.Value <= 0)
                throw new InvalidOperationException("Minimum quantity is required for volume discount");
        }

        Discount? entity = null;
        if (dto.Id > 0)
        {
            entity = await _discountRepository.GetByIdAsync(dto.Id);
        }

        if (entity == null)
        {
            entity = _mapper.Map<Discount>(dto);
            entity.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.DiscountType = dto.DiscountType;
            entity.DiscountValue = dto.DiscountValue;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.ApplicableFor = dto.ApplicableFor;
            entity.MinimumQuantity = dto.MinimumQuantity;
            entity.IsActive = dto.IsActive;
            entity.Notes = dto.Notes;
        }

        entity.UpdatedAt = DateTime.UtcNow;

        if (dto.Id > 0)
        {
            await _discountRepository.UpdateAsync(entity);
        }
        else
        {
            await _discountRepository.AddAsync(entity);
        }

        await _discountRepository.SaveChangesAsync();
        _logger.LogInformation($"Discount saved: {entity.Name}");

        return _mapper.Map<DiscountDto>(entity);
    }

    public async Task<IEnumerable<BundlePackageDto>> GetBundlesAsync(bool includeInactive = true)
    {
        var query = _bundleRepository
            .AsQueryable()
            .Include(b => b.SKU)
            .Where(b => !b.IsDeleted);

        if (!includeInactive)
            query = query.Where(b => b.IsActive);

        var bundles = await query
            .OrderBy(b => b.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<BundlePackageDto>>(bundles);
    }

    public async Task<BundlePackageDto> SaveBundleAsync(BundlePackageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Bundle name is required");

        if (!dto.SKUId.HasValue || dto.SKUId.Value <= 0)
            throw new InvalidOperationException("Bundle SKU is required");

        if (dto.Quantity <= 0)
            throw new InvalidOperationException("Bundle quantity must be greater than 0");

        if (dto.BundlePrice < 0)
            throw new InvalidOperationException("Bundle price must be 0 or higher");

        var sku = await _skuRepository.GetByIdAsync(dto.SKUId.Value);
        if (sku == null || sku.IsDeleted)
            throw new InvalidOperationException("SKU not found");

        BundlePackage? entity = null;
        if (dto.Id > 0)
        {
            entity = await _bundleRepository.GetByIdAsync(dto.Id);
        }

        if (entity == null)
        {
            entity = _mapper.Map<BundlePackage>(dto);
            entity.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.BundlePrice = dto.BundlePrice;
            entity.Quantity = dto.Quantity;
            entity.SKUId = dto.SKUId;
            entity.IsActive = dto.IsActive;
            entity.Notes = dto.Notes;
        }

        entity.UpdatedAt = DateTime.UtcNow;

        if (dto.Id > 0)
        {
            await _bundleRepository.UpdateAsync(entity);
        }
        else
        {
            await _bundleRepository.AddAsync(entity);
        }

        await _bundleRepository.SaveChangesAsync();
        _logger.LogInformation($"Bundle saved: {entity.Name}");

        return _mapper.Map<BundlePackageDto>(entity);
    }
}
