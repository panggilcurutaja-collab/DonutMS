using Microsoft.EntityFrameworkCore;
using DonutMS.Data.DbContext;
using DonutMS.Data.Entities;

namespace DonutMS.Data.Repositories;

public class RecipeRepository : Repository<Recipe>, IRecipeRepository
{
    private readonly DonutMSDbContext _context;

    public RecipeRepository(DonutMSDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Recipe?> GetRecipeWithIngredientsAsync(int id)
    {
        return await _context.Recipes
            .Include(r => r.YieldUnit)
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .ThenInclude(i => i.Prices)
            .ThenInclude(p => p.Unit)
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Unit)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public async Task<IEnumerable<Recipe>> GetActiveRecipesAsync()
    {
        return await _context.Recipes
            .Include(r => r.YieldUnit)
            .Where(r => r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Recipe?> GetByCodeAsync(string code)
    {
        return await _context.Recipes
            .Include(r => r.YieldUnit)
            .FirstOrDefaultAsync(r => r.Code == code && !r.IsDeleted);
    }
}

public class IngredientRepository : Repository<Ingredient>, IIngredientRepository
{
    private readonly DonutMSDbContext _context;

    public IngredientRepository(DonutMSDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Ingredient?> GetIngredientWithPricesAsync(int id)
    {
        return await _context.Ingredients
            .Include(i => i.Prices)
            .ThenInclude(p => p.Supplier)
            .Include(i => i.Prices)
            .ThenInclude(p => p.Unit)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
    }

    public async Task<Ingredient?> GetBySKUAsync(string sku)
    {
        return await _context.Ingredients
            .FirstOrDefaultAsync(i => i.SKU == sku && !i.IsDeleted);
    }

    public async Task<IEnumerable<Ingredient>> GetLowStockIngredientsAsync()
    {
        return await _context.Ingredients
            .Include(i => i.InventoryStocks)
            .Where(i => !i.IsDeleted && i.InventoryStocks != null && i.InventoryStocks.Any(s => s.AvailableQuantity <= i.MinimumStockLevel))
            .ToListAsync();
    }

    public async Task<IEnumerable<Ingredient>> GetExpiredIngredientsAsync()
    {
        var expiredDate = DateTime.UtcNow;
        return await _context.Ingredients
            .Include(i => i.InventoryStocks)
            .ThenInclude(s => s.StockBatches)
            .Where(i => !i.IsDeleted && i.InventoryStocks != null && 
                i.InventoryStocks.Any(s => s.StockBatches != null && 
                s.StockBatches.Any(sb => sb.ExpiryDate < expiredDate)))
            .ToListAsync();
    }
}

public class InventoryRepository : Repository<InventoryStock>, IInventoryRepository
{
    private readonly DonutMSDbContext _context;

    public InventoryRepository(DonutMSDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<InventoryStock?> GetByIngredientIdAsync(int ingredientId)
    {
        return await _context.InventoryStocks
            .Include(s => s.Ingredient)
            .Include(s => s.Unit)
            .Include(s => s.StockBatches)
            .Include(s => s.Transactions)
            .ThenInclude(t => t.Unit)
            .FirstOrDefaultAsync(s => s.IngredientId == ingredientId && !s.IsDeleted);
    }

    public async Task<IEnumerable<StockBatch>> GetExpiringStockAsync(int daysUntilExpiry)
    {
        var expiryDate = DateTime.UtcNow.AddDays(daysUntilExpiry);
        return await _context.StockBatches
            .Where(sb => sb.ExpiryDate.HasValue && sb.ExpiryDate <= expiryDate && sb.AvailableQuantity > 0)
            .OrderBy(sb => sb.ExpiryDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockTransaction>> GetTransactionsAsync(int ingredientId, DateTime fromDate, DateTime toDate)
    {
        return await _context.StockTransactions
            .Include(t => t.Unit)
            .Where(t => t.InventoryStock.IngredientId == ingredientId && t.TransactionDate >= fromDate && t.TransactionDate <= toDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
}

public class ProductionRepository : Repository<Batch>, IProductionRepository
{
    private readonly DonutMSDbContext _context;

    public ProductionRepository(DonutMSDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Batch?> GetBatchWithIngredientsAsync(int id)
    {
        return await _context.Batches
            .Include(b => b.Recipe)
            .Include(b => b.YieldUnit)
            .Include(b => b.Ingredients)
            .ThenInclude(bi => bi.Ingredient)
            .Include(b => b.Ingredients)
            .ThenInclude(bi => bi.PlannedUnit)
            .Include(b => b.QualityControls)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
    }

    public async Task<IEnumerable<Batch>> GetBatchesByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.Batches
            .Include(b => b.Recipe)
            .Include(b => b.YieldUnit)
            .Where(b => b.ProductionDate >= fromDate && b.ProductionDate <= toDate && !b.IsDeleted)
            .OrderByDescending(b => b.ProductionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Batch>> GetActiveBatchesAsync()
    {
        return await _context.Batches
            .Include(b => b.Recipe)
            .Include(b => b.YieldUnit)
            .Where(b => (b.Status == "Planned" || b.Status == "In Progress") && !b.IsDeleted)
            .OrderByDescending(b => b.ProductionDate)
            .ToListAsync();
    }
}

public class PurchaseOrderRepository : Repository<PurchaseOrder>, IPurchaseOrderRepository
{
    private readonly DonutMSDbContext _context;

    public PurchaseOrderRepository(DonutMSDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<PurchaseOrder?> GetWithItemsAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .ThenInclude(poi => poi.Ingredient)
            .Include(po => po.Items)
            .ThenInclude(poi => poi.Unit)
            .Include(po => po.Receivings)
            .ThenInclude(r => r.Details)
            .ThenInclude(d => d.PurchaseOrderItem)
            .ThenInclude(i => i.Ingredient)
            .Include(po => po.Receivings)
            .ThenInclude(r => r.Details)
            .ThenInclude(d => d.PurchaseOrderItem)
            .ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetPendingAsync()
    {
        return await _context.PurchaseOrders
            .Where(po => po.Status != "Received" && po.Status != "Cancelled" && !po.IsDeleted)
            .OrderBy(po => po.RequiredDeliveryDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<PurchaseOrder>> GetBySupplerIdAsync(int supplierId)
    {
        return await _context.PurchaseOrders
            .Where(po => po.SupplierId == supplierId && !po.IsDeleted)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();
    }
}

public class SKURepository : Repository<SKU>, ISKURepository
{
    private readonly DonutMSDbContext _context;

    public SKURepository(DonutMSDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<SKU?> GetWithCostsAsync(int id)
    {
        return await _context.SKUs
            .Include(s => s.Costs)
            .Include(s => s.PriceHistory)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public async Task<SKU?> GetByCodeAsync(string code)
    {
        return await _context.SKUs
            .FirstOrDefaultAsync(s => s.Code == code && !s.IsDeleted);
    }

    public async Task<IEnumerable<SKUCost>> GetCurrentCostsAsync()
    {
        var today = DateTime.UtcNow;
        return await _context.SKUCosts
            .Where(sc => sc.EffectiveDate <= today && (!sc.EndDate.HasValue || sc.EndDate >= today) && sc.IsActive)
            .ToListAsync();
    }
}

public class AuditRepository : Repository<AuditLog>, IAuditRepository
{
    private readonly DonutMSDbContext _context;

    public AuditRepository(DonutMSDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string entityName, int entityId)
    {
        return await _context.AuditLogs
            .Where(al => al.EntityName == entityName && al.EntityId == entityId)
            .OrderByDescending(al => al.AuditDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditsByUserAsync(string userId)
    {
        return await _context.AuditLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.AuditDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditsByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.AuditLogs
            .Where(al => al.AuditDate >= fromDate && al.AuditDate <= toDate)
            .OrderByDescending(al => al.AuditDate)
            .ToListAsync();
    }
}
