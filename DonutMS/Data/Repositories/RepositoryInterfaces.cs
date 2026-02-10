using DonutMS.Data.Entities;

namespace DonutMS.Data.Repositories;

public interface IRecipeRepository : IRepository<Recipe>
{
    Task<Recipe?> GetRecipeWithIngredientsAsync(int id);
    Task<IEnumerable<Recipe>> GetActiveRecipesAsync();
    Task<Recipe?> GetByCodeAsync(string code);
}

public interface IIngredientRepository : IRepository<Ingredient>
{
    Task<Ingredient?> GetIngredientWithPricesAsync(int id);
    Task<Ingredient?> GetBySKUAsync(string sku);
    Task<IEnumerable<Ingredient>> GetLowStockIngredientsAsync();
    Task<IEnumerable<Ingredient>> GetExpiredIngredientsAsync();
}

public interface IInventoryRepository : IRepository<InventoryStock>
{
    Task<InventoryStock?> GetByIngredientIdAsync(int ingredientId);
    Task<IEnumerable<StockBatch>> GetExpiringStockAsync(int daysUntilExpiry);
    Task<IEnumerable<StockTransaction>> GetTransactionsAsync(int ingredientId, DateTime fromDate, DateTime toDate);
}

public interface IProductionRepository : IRepository<Batch>
{
    Task<Batch?> GetBatchWithIngredientsAsync(int id);
    Task<IEnumerable<Batch>> GetBatchesByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<IEnumerable<Batch>> GetActiveBatchesAsync();
}

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetWithItemsAsync(int id);
    Task<IEnumerable<PurchaseOrder>> GetPendingAsync();
    Task<IEnumerable<PurchaseOrder>> GetBySupplerIdAsync(int supplierId);
}

public interface ISKURepository : IRepository<SKU>
{
    Task<SKU?> GetWithCostsAsync(int id);
    Task<SKU?> GetByCodeAsync(string code);
    Task<IEnumerable<SKUCost>> GetCurrentCostsAsync();
}

public interface IAuditRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string entityName, int entityId);
    Task<IEnumerable<AuditLog>> GetAuditsByUserAsync(string userId);
    Task<IEnumerable<AuditLog>> GetAuditsByDateRangeAsync(DateTime fromDate, DateTime toDate);
}
