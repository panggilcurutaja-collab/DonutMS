using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Core.Domain;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

// ========== INVENTORY SERVICE ==========
public interface IInventoryService
{
    Task<InventoryStockDto?> GetStockByIngredientIdAsync(int ingredientId);
    Task<IEnumerable<StockBatchDto>> GetExpiringStockAsync(int daysUntilExpiry);
    Task<StockTransactionDto> AddStockInAsync(
        int ingredientId,
        decimal quantity,
        int unitId,
        DateTime? receiptDate = null,
        DateTime? expiryDate = null,
        string? batchNumber = null,
        string? referenceNumber = null,
        int? purchaseOrderId = null);
    Task<IEnumerable<StockTransactionDto>> RemoveStockOutAsync(
        int ingredientId,
        decimal quantity,
        int unitId,
        bool useFefo = true,
        int? batchId = null,
        string? referenceNumber = null,
        int? productionBatchId = null);
    Task<IEnumerable<StockTransactionDto>> GetTransactionsAsync(int ingredientId, DateTime fromDate, DateTime toDate);
    Task<IEnumerable<StockTransactionDto>> GetTransactionsByBatchIdAsync(int batchId);
    Task<IEnumerable<InventoryStockDto>> GetLowStockItemsAsync();
    Task<bool> ReserveStockAsync(int ingredientId, decimal quantity);
    Task<bool> ReleaseReservedStockAsync(int ingredientId, decimal quantity);
}

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitConversionService _unitConversionService;
    private readonly ILogger<InventoryService> _logger;
    private readonly IMapper _mapper;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IIngredientRepository ingredientRepository,
        IUnitConversionService unitConversionService,
        ILogger<InventoryService> logger,
        IMapper mapper)
    {
        _inventoryRepository = inventoryRepository;
        _ingredientRepository = ingredientRepository;
        _unitConversionService = unitConversionService;
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

    public async Task<StockTransactionDto> AddStockInAsync(
        int ingredientId,
        decimal quantity,
        int unitId,
        DateTime? receiptDate = null,
        DateTime? expiryDate = null,
        string? batchNumber = null,
        string? referenceNumber = null,
        int? purchaseOrderId = null)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than 0");

        var stock = await _inventoryRepository.GetByIngredientIdAsync(ingredientId);
        if (stock == null)
        {
            var ingredient = await _ingredientRepository.GetByIdAsync(ingredientId);
            if (ingredient == null)
                throw new KeyNotFoundException($"Ingredient {ingredientId} not found");

            stock = new InventoryStock
            {
                IngredientId = ingredientId,
                UnitId = ingredient.ConsumptionUnitId,
                Quantity = 0,
                ReservedQuantity = 0,
                LastUpdated = DateTime.UtcNow
            };

            await _inventoryRepository.AddAsync(stock);
            await _inventoryRepository.SaveChangesAsync();
        }

        var normalizedQuantity = await NormalizeQuantityAsync(stock, quantity, unitId);
        var receivedAt = receiptDate ?? DateTime.UtcNow;
        var calculatedExpiry = expiryDate;

        if (!calculatedExpiry.HasValue)
        {
            var ingredient = await _ingredientRepository.GetByIdAsync(ingredientId);
            if (ingredient != null && ingredient.ShelfLifeDays > 0)
            {
                calculatedExpiry = receivedAt.Date.AddDays(ingredient.ShelfLifeDays);
            }
        }

        var transaction = new StockTransaction
        {
            InventoryStockId = stock.Id,
            TransactionType = DomainConstants.StockTransactionType.In,
            Quantity = normalizedQuantity,
            UnitId = stock.UnitId,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = referenceNumber,
            PurchaseOrderId = purchaseOrderId
        };

        var batch = new StockBatch
        {
            InventoryStockId = stock.Id,
            SupplierBatchNumber = batchNumber,
            ReceiptDate = receivedAt,
            ExpiryDate = calculatedExpiry,
            QuantityReceived = normalizedQuantity,
            QuantityUsed = 0,
            QuantityWasted = 0,
            Status = DomainConstants.StockBatchStatus.Active
        };

        transaction.StockBatch = batch;

        stock.StockBatches ??= new List<StockBatch>();
        stock.StockBatches.Add(batch);

        stock.Transactions ??= new List<StockTransaction>();
        stock.Transactions.Add(transaction);

        stock.Quantity += normalizedQuantity;
        stock.LastUpdated = DateTime.UtcNow;

        await _inventoryRepository.UpdateAsync(stock);
        await _inventoryRepository.SaveChangesAsync();

        _logger.LogInformation($"Stock in: {normalizedQuantity} units added to ingredient {ingredientId}");
        return _mapper.Map<StockTransactionDto>(transaction);
    }

    public async Task<IEnumerable<StockTransactionDto>> RemoveStockOutAsync(
        int ingredientId,
        decimal quantity,
        int unitId,
        bool useFefo = true,
        int? batchId = null,
        string? referenceNumber = null,
        int? productionBatchId = null)
    {
        var stock = await _inventoryRepository.GetByIngredientIdAsync(ingredientId);
        if (stock == null)
            throw new KeyNotFoundException($"Stock for ingredient {ingredientId} not found");

        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than 0");

        var normalizedQuantity = await NormalizeQuantityAsync(stock, quantity, unitId);

        if (stock.AvailableQuantity < normalizedQuantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {stock.AvailableQuantity}, Requested: {normalizedQuantity}");

        var transactions = new List<StockTransaction>();
        var remaining = normalizedQuantity;

        if (batchId.HasValue && stock.StockBatches != null)
        {
            var selectedBatch = stock.StockBatches.FirstOrDefault(b => b.Id == batchId.Value);
            if (selectedBatch == null)
                throw new InvalidOperationException("Selected batch not found");

            if (selectedBatch.AvailableQuantity < remaining)
                throw new InvalidOperationException("Selected batch does not have enough quantity");

            ConsumeBatch(stock, selectedBatch, remaining, referenceNumber, productionBatchId, transactions);
            remaining = 0;
        }
        else if (stock.StockBatches != null && stock.StockBatches.Any())
        {
            var orderedBatches = useFefo
                ? stock.StockBatches
                    .Where(b => b.AvailableQuantity > 0)
                    .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                    .ThenBy(b => b.ReceiptDate)
                : stock.StockBatches
                    .Where(b => b.AvailableQuantity > 0)
                    .OrderBy(b => b.ReceiptDate);

            foreach (var batch in orderedBatches)
            {
                if (remaining <= 0)
                    break;

                var take = Math.Min(batch.AvailableQuantity, remaining);
                ConsumeBatch(stock, batch, take, referenceNumber, productionBatchId, transactions);
                remaining -= take;
            }
        }
        else
        {
            var transaction = new StockTransaction
            {
                InventoryStockId = stock.Id,
                TransactionType = DomainConstants.StockTransactionType.Out,
                Quantity = remaining,
                UnitId = stock.UnitId,
                TransactionDate = DateTime.UtcNow,
                ReferenceNumber = referenceNumber,
                BatchId = productionBatchId
            };

            stock.Transactions ??= new List<StockTransaction>();
            stock.Transactions.Add(transaction);
            transactions.Add(transaction);
            remaining = 0;
        }

        stock.Quantity -= normalizedQuantity;
        stock.LastUpdated = DateTime.UtcNow;

        await _inventoryRepository.UpdateAsync(stock);
        await _inventoryRepository.SaveChangesAsync();

        _logger.LogInformation($"Stock out: {normalizedQuantity} units removed from ingredient {ingredientId}");
        return _mapper.Map<IEnumerable<StockTransactionDto>>(transactions);
    }

    public async Task<IEnumerable<StockTransactionDto>> GetTransactionsAsync(int ingredientId, DateTime fromDate, DateTime toDate)
    {
        var transactions = await _inventoryRepository.GetTransactionsAsync(ingredientId, fromDate, toDate);
        return _mapper.Map<IEnumerable<StockTransactionDto>>(transactions);
    }

    public async Task<IEnumerable<StockTransactionDto>> GetTransactionsByBatchIdAsync(int batchId)
    {
        var transactions = await _inventoryRepository.GetTransactionsByBatchIdAsync(batchId);
        return _mapper.Map<IEnumerable<StockTransactionDto>>(transactions);
    }

    public async Task<IEnumerable<InventoryStockDto>> GetLowStockItemsAsync()
    {
        var lowStocks = await _inventoryRepository
            .AsQueryable()
            .Include(s => s.Ingredient)
            .Include(s => s.Unit)
            .Where(s => !s.IsDeleted && s.Ingredient != null &&
                (s.Quantity - s.ReservedQuantity) <= s.Ingredient.MinimumStockLevel)
            .ToListAsync();

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

    private async Task<decimal> NormalizeQuantityAsync(InventoryStock stock, decimal quantity, int unitId)
    {
        if (stock.UnitId == unitId)
            return quantity;

        return await _unitConversionService.ConvertAsync(quantity, unitId, stock.UnitId);
    }

    private static void ConsumeBatch(
        InventoryStock stock,
        StockBatch batch,
        decimal quantity,
        string? referenceNumber,
        int? productionBatchId,
        List<StockTransaction> transactions)
    {
        batch.QuantityUsed += quantity;
        if (batch.AvailableQuantity <= 0)
        {
            batch.Status = DomainConstants.StockBatchStatus.Depleted;
        }

        var transaction = new StockTransaction
        {
            InventoryStockId = stock.Id,
            StockBatchId = batch.Id,
            TransactionType = DomainConstants.StockTransactionType.Out,
            Quantity = quantity,
            UnitId = stock.UnitId,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = referenceNumber,
            BatchId = productionBatchId
        };

        stock.Transactions ??= new List<StockTransaction>();
        stock.Transactions.Add(transaction);
        transactions.Add(transaction);
    }
}

// ========== PRODUCTION SERVICE ==========
public interface IProductionService
{
    Task<BatchDto> CreateBatchAsync(CreateBatchDto dto);
    Task<BatchDto?> GetBatchByIdAsync(int id);
    Task<IEnumerable<BatchDto>> GetActiveBatchesAsync();
    Task<IEnumerable<BatchDto>> GetBatchesByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<bool> StartBatchProductionAsync(int batchId);
    Task<bool> CompleteBatchAsync(int batchId, decimal actualYield, decimal? wasteQuantity = null, string? notes = null);
    Task<QualityControlDto> AddQualityControlAsync(int batchId, QualityControlDto qcDto);
}

public class ProductionService : IProductionService
{
    private readonly IProductionRepository _productionRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRepository<QualityControl> _qualityControlRepository;
    private readonly ILogger<ProductionService> _logger;
    private readonly IMapper _mapper;

    public ProductionService(
        IProductionRepository productionRepository,
        IInventoryService inventoryService,
        IRecipeRepository recipeRepository,
        IRepository<QualityControl> qualityControlRepository,
        ILogger<ProductionService> logger,
        IMapper mapper)
    {
        _productionRepository = productionRepository;
        _inventoryService = inventoryService;
        _recipeRepository = recipeRepository;
        _qualityControlRepository = qualityControlRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<BatchDto> CreateBatchAsync(CreateBatchDto dto)
    {
        var recipe = await _recipeRepository.GetRecipeWithIngredientsAsync(dto.RecipeId);
        if (recipe == null)
            throw new KeyNotFoundException($"Recipe {dto.RecipeId} not found");

        var targetYield = dto.TargetYield > 0 ? dto.TargetYield : recipe.YieldPerBatch;
        if (targetYield <= 0)
            throw new InvalidOperationException("Target yield must be greater than 0");

        var batch = _mapper.Map<Batch>(dto);
        batch.Status = DomainConstants.BatchStatus.Planned;
        batch.TargetYield = targetYield;
        batch.YieldUnitId = recipe.YieldUnitId;
        batch.RecipeVersionId = recipe.CurrentVersionId;

        var baseYield = recipe.YieldPerBatch <= 0 ? 1 : recipe.YieldPerBatch;
        var scaleFactor = targetYield / baseYield;

        var ingredients = new List<BatchIngredient>();
        if (recipe.RecipeIngredients != null)
        {
            foreach (var ingredient in recipe.RecipeIngredients.OrderBy(i => i.SortOrder))
            {
                var plannedQty = ingredient.QuantityPerBatch * scaleFactor;
                ingredients.Add(new BatchIngredient
                {
                    IngredientId = ingredient.IngredientId,
                    PlannedQuantity = plannedQty,
                    PlannedUnitId = ingredient.UnitId,
                    Status = DomainConstants.BatchIngredientStatus.Planned
                });
            }
        }

        batch.Ingredients = ingredients;

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

    public async Task<IEnumerable<BatchDto>> GetBatchesByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var batches = await _productionRepository.GetBatchesByDateRangeAsync(fromDate, toDate);
        return _mapper.Map<IEnumerable<BatchDto>>(batches);
    }

    public async Task<bool> StartBatchProductionAsync(int batchId)
    {
        var batch = await _productionRepository.GetBatchWithIngredientsAsync(batchId);
        if (batch == null || batch.Status != DomainConstants.BatchStatus.Planned)
            return false;

        var ingredients = batch.Ingredients ?? new List<BatchIngredient>();
        if (!ingredients.Any())
        {
            var recipe = await _recipeRepository.GetRecipeWithIngredientsAsync(batch.RecipeId);
            if (recipe != null && recipe.RecipeIngredients != null)
            {
                var baseYield = recipe.YieldPerBatch <= 0 ? 1 : recipe.YieldPerBatch;
                var scaleFactor = batch.TargetYield / baseYield;

                ingredients = recipe.RecipeIngredients
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new BatchIngredient
                    {
                        IngredientId = i.IngredientId,
                        PlannedQuantity = i.QuantityPerBatch * scaleFactor,
                        PlannedUnitId = i.UnitId,
                    Status = DomainConstants.BatchIngredientStatus.Planned
                })
                .ToList();

                batch.Ingredients = ingredients;
            }
        }
        foreach (var ingredient in ingredients)
        {
            if (ingredient.PlannedQuantity <= 0)
                continue;

            await _inventoryService.RemoveStockOutAsync(
                ingredient.IngredientId,
                ingredient.PlannedQuantity,
                ingredient.PlannedUnitId,
                true,
                null,
                batch.BatchCode,
                batch.Id);

            ingredient.ActualQuantity = ingredient.PlannedQuantity;
            ingredient.Status = DomainConstants.BatchIngredientStatus.Allocated;
        }

        batch.Status = DomainConstants.BatchStatus.InProgress;
        await _productionRepository.UpdateAsync(batch);
        await _productionRepository.SaveChangesAsync();

        _logger.LogInformation($"Batch '{batch.BatchCode}' production started");
        return true;
    }

    public async Task<bool> CompleteBatchAsync(int batchId, decimal actualYield, decimal? wasteQuantity = null, string? notes = null)
    {
        var batch = await _productionRepository.GetBatchWithIngredientsAsync(batchId);
        if (batch == null)
            return false;

        batch.Status = DomainConstants.BatchStatus.Completed;
        batch.ActualYield = actualYield;
        var computedWaste = Math.Max(batch.TargetYield - actualYield, 0);
        batch.WasteQuantity = wasteQuantity.HasValue ? Math.Max(wasteQuantity.Value, 0) : computedWaste;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            batch.Notes = notes;
        }

        if (batch.Ingredients != null)
        {
            foreach (var ingredient in batch.Ingredients)
            {
                if (ingredient.Status == DomainConstants.BatchIngredientStatus.Allocated)
                {
                    ingredient.Status = DomainConstants.BatchIngredientStatus.Consumed;
                }
            }
        }

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
        var scoreCount = 0;
        decimal totalScore = 0;
        if (qc.Taste.HasValue) { totalScore += qc.Taste.Value; scoreCount++; }
        if (qc.Texture.HasValue) { totalScore += qc.Texture.Value; scoreCount++; }
        if (qc.Appearance.HasValue) { totalScore += qc.Appearance.Value; scoreCount++; }
        if (qc.Aroma.HasValue) { totalScore += qc.Aroma.Value; scoreCount++; }

        var averageScore = scoreCount > 0 ? totalScore / scoreCount : 0;
        qc.Passed = averageScore >= 7;
        batch.HasQCPass = qc.Passed;

        await _qualityControlRepository.AddAsync(qc);
        await _qualityControlRepository.SaveChangesAsync();
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
    Task<decimal> CalculateMaterialCostAsync(int recipeId, decimal wastePercent);
    Task<decimal> CalculateHPPAsync(int skuId);
    Task<SKUCostDto> CalculateSKUCostAsync(int skuId, decimal materialCost, decimal laborCost, decimal overheadCost);
    Task<RecipeCostBreakdownDto> CalculateRecipeCostBreakdownAsync(
        int recipeId,
        decimal wastePercent,
        decimal packagingCost,
        decimal laborCost,
        decimal overheadCost);
}

public class CostCalculationService : ICostCalculationService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ISKURepository _skuRepository;
    private readonly IUnitConversionService _unitConversionService;
    private readonly ILogger<CostCalculationService> _logger;
    private readonly IMapper _mapper;

    public CostCalculationService(
        IRecipeRepository recipeRepository,
        ISKURepository skuRepository,
        IUnitConversionService unitConversionService,
        ILogger<CostCalculationService> logger,
        IMapper mapper)
    {
        _recipeRepository = recipeRepository;
        _skuRepository = skuRepository;
        _unitConversionService = unitConversionService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<decimal> CalculateMaterialCostAsync(int recipeId)
    {
        return await CalculateMaterialCostAsync(recipeId, 0m);
    }

    public async Task<decimal> CalculateMaterialCostAsync(int recipeId, decimal wastePercent)
    {
        var breakdown = await CalculateRecipeCostBreakdownAsync(
            recipeId,
            wastePercent,
            packagingCost: 0m,
            laborCost: 0m,
            overheadCost: 0m);

        return breakdown.MaterialCost;
    }

    public async Task<decimal> CalculateHPPAsync(int skuId)
    {
        var costs = await _skuRepository.GetCurrentCostsAsync();
        var skuCost = costs.FirstOrDefault(c => c.SKUId == skuId);

        if (skuCost != null)
            return skuCost.MaterialCost + skuCost.PackagingCost + skuCost.LaborCost + skuCost.OverheadCost;

        var sku = await _skuRepository
            .AsQueryable()
            .Include(s => s.Recipe)
            .FirstOrDefaultAsync(s => s.Id == skuId && !s.IsDeleted);

        if (sku?.RecipeId == null)
            return 0;

        var breakdown = await CalculateRecipeCostBreakdownAsync(
            sku.RecipeId.Value,
            wastePercent: 0m,
            packagingCost: 0m,
            laborCost: 0m,
            overheadCost: 0m);

        return breakdown.HppPerUnit;
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

    public async Task<RecipeCostBreakdownDto> CalculateRecipeCostBreakdownAsync(
        int recipeId,
        decimal wastePercent,
        decimal packagingCost,
        decimal laborCost,
        decimal overheadCost)
    {
        var recipe = await _recipeRepository.GetRecipeWithIngredientsAsync(recipeId);
        if (recipe == null)
        {
            return new RecipeCostBreakdownDto
            {
                RecipeId = recipeId,
                WastePercent = wastePercent
            };
        }

        var ingredientLines = new List<IngredientCostLineDto>();
        decimal materialCost = 0m;

        if (recipe.RecipeIngredients != null)
        {
            foreach (var ingredient in recipe.RecipeIngredients.OrderBy(i => i.SortOrder))
            {
                var line = new IngredientCostLineDto
                {
                    IngredientId = ingredient.IngredientId,
                    IngredientName = ingredient.Ingredient?.Name ?? "(Unknown)",
                    QuantityPerBatch = ingredient.QuantityPerBatch,
                    UnitId = ingredient.UnitId,
                    UnitCode = ingredient.Unit?.Code ?? string.Empty,
                    HasPrice = false
                };

                var currentPrice = ingredient.Ingredient?.Prices?
                    .Where(p => p.IsActive && p.EffectiveDate <= DateTime.UtcNow && (!p.EndDate.HasValue || p.EndDate >= DateTime.UtcNow))
                    .OrderByDescending(p => p.EffectiveDate)
                    .FirstOrDefault();

                if (currentPrice == null)
                {
                    line.Error = "Missing active price";
                    ingredientLines.Add(line);
                    continue;
                }

                try
                {
                    decimal quantityInPriceUnit = ingredient.QuantityPerBatch;

                    if (ingredient.UnitId != currentPrice.UnitId)
                    {
                        quantityInPriceUnit = await _unitConversionService.ConvertAsync(
                            ingredient.QuantityPerBatch,
                            ingredient.UnitId,
                            currentPrice.UnitId);
                    }

                    var baseCost = currentPrice.Price * quantityInPriceUnit;
                    var costWithWaste = baseCost * (1 + (wastePercent / 100m));

                    line.PricePerUnit = currentPrice.Price;
                    line.PriceUnitId = currentPrice.UnitId;
                    line.PriceUnitCode = currentPrice.Unit?.Code ?? string.Empty;
                    line.Cost = baseCost;
                    line.CostWithWaste = costWithWaste;
                    line.HasPrice = true;

                    materialCost += costWithWaste;
                }
                catch (Exception ex)
                {
                    line.Error = $"Conversion failed: {ex.Message}";
                }

                ingredientLines.Add(line);
            }
        }

        var totalCost = materialCost + packagingCost + laborCost + overheadCost;
        var yieldPerBatch = recipe.YieldPerBatch <= 0 ? 1 : recipe.YieldPerBatch;
        var hppPerUnit = totalCost / yieldPerBatch;

        return new RecipeCostBreakdownDto
        {
            RecipeId = recipe.Id,
            RecipeName = recipe.Name,
            YieldPerBatch = recipe.YieldPerBatch,
            WastePercent = wastePercent,
            MaterialCost = materialCost,
            PackagingCost = packagingCost,
            LaborCost = laborCost,
            OverheadCost = overheadCost,
            TotalCost = totalCost,
            HppPerUnit = hppPerUnit,
            IngredientCosts = ingredientLines
        };
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
