using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

// ========== INVENTORY SERVICE ==========
public interface IInventoryService
{
    Task<InventoryStockDto?> GetStockByIngredientIdAsync(int ingredientId);
    Task<IEnumerable<StockBatchDto>> GetExpiringStockAsync(int daysUntilExpiry);
    Task<StockTransactionDto> AddStockInAsync(int ingredientId, decimal quantity, int unitId, string? referenceNumber = null);
    Task<StockTransactionDto> RemoveStockOutAsync(int ingredientId, decimal quantity, int unitId, int? batchId = null);
    Task<IEnumerable<InventoryStockDto>> GetLowStockItemsAsync();
    Task<bool> ReserveStockAsync(int ingredientId, decimal quantity);
    Task<bool> ReleaseReservedStockAsync(int ingredientId, decimal quantity);
}

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<InventoryService> _logger;
    private readonly IMapper _mapper;

    public InventoryService(IInventoryRepository inventoryRepository, ILogger<InventoryService> logger, IMapper mapper)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<InventoryStockDto?> GetStockByIngredientIdAsync(int ingredientId)
    {
        var stock = await _inventoryRepository.GetByIngredientIdAsync(ingredientId);
        return stock != null ? _mapper.Map<InventoryStockDto>(stock) : null;
    }

    public async Task<IEnumerable<StockBatchDto>> GetExpiringStockAsync(int daysUntilExpiry)
    {
        var batches = await _inventoryRepository.GetExpiringStockAsync(daysUntilExpiry);
        return _mapper.Map<IEnumerable<StockBatchDto>>(batches);
    }

    public async Task<StockTransactionDto> AddStockInAsync(int ingredientId, decimal quantity, int unitId, string? referenceNumber = null)
    {
        var stock = await _inventoryRepository.GetByIngredientIdAsync(ingredientId);
        if (stock == null)
            throw new KeyNotFoundException($"Stock for ingredient {ingredientId} not found");

        var transaction = new StockTransaction
        {
            InventoryStockId = stock.Id,
            TransactionType = "In",
            Quantity = quantity,
            UnitId = unitId,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = referenceNumber
        };

        stock.Quantity += quantity;
        await _inventoryRepository.UpdateAsync(stock);
        
        // Add transaction record
        var stockTransaction = new List<StockTransaction> { transaction };
        await _inventoryRepository.SaveChangesAsync();

        _logger.LogInformation($"Stock in: {quantity} units added to ingredient {ingredientId}");
        return _mapper.Map<StockTransactionDto>(transaction);
    }

    public async Task<StockTransactionDto> RemoveStockOutAsync(int ingredientId, decimal quantity, int unitId, int? batchId = null)
    {
        var stock = await _inventoryRepository.GetByIngredientIdAsync(ingredientId);
        if (stock == null)
            throw new KeyNotFoundException($"Stock for ingredient {ingredientId} not found");

        if (stock.AvailableQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {stock.AvailableQuantity}, Requested: {quantity}");

        var transaction = new StockTransaction
        {
            InventoryStockId = stock.Id,
            TransactionType = "Out",
            Quantity = quantity,
            UnitId = unitId,
            TransactionDate = DateTime.UtcNow,
            BatchId = batchId
        };

        stock.Quantity -= quantity;
        await _inventoryRepository.UpdateAsync(stock);
        await _inventoryRepository.SaveChangesAsync();

        _logger.LogInformation($"Stock out: {quantity} units removed from ingredient {ingredientId}");
        return _mapper.Map<StockTransactionDto>(transaction);
    }

    public async Task<IEnumerable<InventoryStockDto>> GetLowStockItemsAsync()
    {
        var allStocks = await _inventoryRepository.GetAllAsync();
        var lowStocks = allStocks
            .Where(s => s.AvailableQuantity <= s.Ingredient!.MinimumStockLevel)
            .ToList();

        return _mapper.Map<IEnumerable<InventoryStockDto>>(lowStocks);
    }

    public async Task<bool> ReserveStockAsync(int ingredientId, decimal quantity)
    {
        var stock = await _inventoryRepository.GetByIngredientIdAsync(ingredientId);
        if (stock == null || stock.AvailableQuantity < quantity)
            return false;

        stock.ReservedQuantity += quantity;
        await _inventoryRepository.UpdateAsync(stock);
        await _inventoryRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReleaseReservedStockAsync(int ingredientId, decimal quantity)
    {
        var stock = await _inventoryRepository.GetByIngredientIdAsync(ingredientId);
        if (stock == null || stock.ReservedQuantity < quantity)
            return false;

        stock.ReservedQuantity -= quantity;
        await _inventoryRepository.UpdateAsync(stock);
        await _inventoryRepository.SaveChangesAsync();

        return true;
    }
}

// ========== PRODUCTION SERVICE ==========
public interface IProductionService
{
    Task<BatchDto> CreateBatchAsync(CreateBatchDto dto);
    Task<BatchDto?> GetBatchByIdAsync(int id);
    Task<IEnumerable<BatchDto>> GetActiveBatchesAsync();
    Task<bool> StartBatchProductionAsync(int batchId);
    Task<bool> CompleteBatchAsync(int batchId, decimal actualYield);
    Task<QualityControlDto> AddQualityControlAsync(int batchId, QualityControlDto qcDto);
}

public class ProductionService : IProductionService
{
    private readonly IProductionRepository _productionRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ProductionService> _logger;
    private readonly IMapper _mapper;

    public ProductionService(
        IProductionRepository productionRepository,
        IInventoryService inventoryService,
        ILogger<ProductionService> logger,
        IMapper mapper)
    {
        _productionRepository = productionRepository;
        _inventoryService = inventoryService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<BatchDto> CreateBatchAsync(CreateBatchDto dto)
    {
        var batch = _mapper.Map<Batch>(dto);
        batch.Status = "Planned";

        await _productionRepository.AddAsync(batch);
        await _productionRepository.SaveChangesAsync();

        _logger.LogInformation($"Batch '{batch.BatchCode}' created");
        return _mapper.Map<BatchDto>(batch);
    }

    public async Task<BatchDto?> GetBatchByIdAsync(int id)
    {
        var batch = await _productionRepository.GetBatchWithIngredientsAsync(id);
        return batch != null ? _mapper.Map<BatchDto>(batch) : null;
    }

    public async Task<IEnumerable<BatchDto>> GetActiveBatchesAsync()
    {
        var batches = await _productionRepository.GetActiveBatchesAsync();
        return _mapper.Map<IEnumerable<BatchDto>>(batches);
    }

    public async Task<bool> StartBatchProductionAsync(int batchId)
    {
        var batch = await _productionRepository.GetByIdAsync(batchId);
        if (batch == null || batch.Status != "Planned")
            return false;

        batch.Status = "In Progress";
        await _productionRepository.UpdateAsync(batch);
        await _productionRepository.SaveChangesAsync();

        _logger.LogInformation($"Batch '{batch.BatchCode}' production started");
        return true;
    }

    public async Task<bool> CompleteBatchAsync(int batchId, decimal actualYield)
    {
        var batch = await _productionRepository.GetByIdAsync(batchId);
        if (batch == null)
            return false;

        batch.Status = "Completed";
        batch.ActualYield = actualYield;
        batch.WasteQuantity = batch.TargetYield - actualYield;

        await _productionRepository.UpdateAsync(batch);
        await _productionRepository.SaveChangesAsync();

        _logger.LogInformation($"Batch '{batch.BatchCode}' completed. Yield: {actualYield}, Waste: {batch.WasteQuantity}");
        return true;
    }

    public async Task<QualityControlDto> AddQualityControlAsync(int batchId, QualityControlDto qcDto)
    {
        var batch = await _productionRepository.GetByIdAsync(batchId);
        if (batch == null)
            throw new KeyNotFoundException($"Batch {batchId} not found");

        var qc = _mapper.Map<QualityControl>(qcDto);
        qc.BatchId = batchId;
        qc.InspectionDate = DateTime.UtcNow;

        // Mock: assume QC passes if avg score > 7
        batch.HasQCPass = (qc.Taste + qc.Texture + qc.Appearance + qc.Aroma) / 4 >= 7;

        await _productionRepository.UpdateAsync(batch);
        await _productionRepository.SaveChangesAsync();

        _logger.LogInformation($"QC record added for batch {batchId}");
        return _mapper.Map<QualityControlDto>(qc);
    }
}

// ========== COST CALCULATION SERVICE ==========
public interface ICostCalculationService
{
    Task<decimal> CalculateMaterialCostAsync(int recipeId);
    Task<decimal> CalculateHPPAsync(int skuId);
    Task<SKUCostDto> CalculateSKUCostAsync(int skuId, decimal materialCost, decimal laborCost, decimal overheadCost);
}

public class CostCalculationService : ICostCalculationService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ISKURepository _skuRepository;
    private readonly ILogger<CostCalculationService> _logger;
    private readonly IMapper _mapper;

    public CostCalculationService(
        IRecipeRepository recipeRepository,
        ISKURepository skuRepository,
        ILogger<CostCalculationService> logger,
        IMapper mapper)
    {
        _recipeRepository = recipeRepository;
        _skuRepository = skuRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<decimal> CalculateMaterialCostAsync(int recipeId)
    {
        var recipe = await _recipeRepository.GetRecipeWithIngredientsAsync(recipeId);
        if (recipe == null || recipe.RecipeIngredients == null)
            return 0;

        decimal totalCost = 0;
        foreach (var ingredient in recipe.RecipeIngredients)
        {
            if (ingredient.Ingredient?.Prices == null)
                continue;

            var currentPrice = ingredient.Ingredient.Prices
                .Where(p => p.EffectiveDate <= DateTime.UtcNow && (!p.EndDate.HasValue || p.EndDate >= DateTime.UtcNow) && p.IsActive)
                .OrderByDescending(p => p.EffectiveDate)
                .FirstOrDefault();

            if (currentPrice != null)
            {
                totalCost += currentPrice.Price * ingredient.QuantityPerBatch;
            }
        }

        return totalCost;
    }

    public async Task<decimal> CalculateHPPAsync(int skuId)
    {
        var costs = await _skuRepository.GetCurrentCostsAsync();
        var skuCost = costs.FirstOrDefault(c => c.SKUId == skuId);

        if (skuCost == null)
            return 0;

        return skuCost.MaterialCost + skuCost.PackagingCost + skuCost.LaborCost + skuCost.OverheadCost;
    }

    public async Task<SKUCostDto> CalculateSKUCostAsync(int skuId, decimal materialCost, decimal laborCost, decimal overheadCost)
    {
        var skuCost = new SKUCost
        {
            SKUId = skuId,
            MaterialCost = materialCost,
            PackagingCost = 0,
            LaborCost = laborCost,
            OverheadCost = overheadCost,
            TotalHPP = materialCost + laborCost + overheadCost,
            EffectiveDate = DateTime.UtcNow,
            IsActive = true
        };

        _logger.LogInformation($"SKU cost calculated: HPP = {skuCost.TotalHPP}");
        return _mapper.Map<SKUCostDto>(skuCost);
    }
}

// ========== PRICING SERVICE ==========
public interface IPricingService
{
    Task<decimal> CalculateSellingPriceAsync(int skuId, decimal marginPercentage);
    Task<decimal> CalculateMarkupAsync(decimal hpp, decimal markupPercentage);
    Task<(decimal GrossMargin, decimal NetMargin)> CalculateMarginsAsync(int skuId, decimal sellingPrice);
    Task<decimal> CalculateBreakEvenAsync(int skuId);
}

public class PricingService : IPricingService
{
    private readonly ISKURepository _skuRepository;
    private readonly ICostCalculationService _costCalculationService;
    private readonly ILogger<PricingService> _logger;

    public PricingService(
        ISKURepository skuRepository,
        ICostCalculationService costCalculationService,
        ILogger<PricingService> logger)
    {
        _skuRepository = skuRepository;
        _costCalculationService = costCalculationService;
        _logger = logger;
    }

    public async Task<decimal> CalculateSellingPriceAsync(int skuId, decimal marginPercentage)
    {
        var hpp = await _costCalculationService.CalculateHPPAsync(skuId);
        var sellingPrice = hpp * (1 + (marginPercentage / 100));

        _logger.LogInformation($"Selling price calculated for SKU {skuId}: {sellingPrice}");
        return sellingPrice;
    }

    public async Task<decimal> CalculateMarkupAsync(decimal hpp, decimal markupPercentage)
    {
        return hpp * (1 + (markupPercentage / 100));
    }

    public async Task<(decimal GrossMargin, decimal NetMargin)> CalculateMarginsAsync(int skuId, decimal sellingPrice)
    {
        var hpp = await _costCalculationService.CalculateHPPAsync(skuId);
        var grossMargin = sellingPrice - hpp;
        var grossMarginPercent = (grossMargin / sellingPrice) * 100;
        var netMargin = grossMarginPercent - 5; // Simplified: assume 5% operational costs

        return (grossMarginPercent, netMargin);
    }

    public async Task<decimal> CalculateBreakEvenAsync(int skuId)
    {
        return await _costCalculationService.CalculateHPPAsync(skuId);
    }
}
