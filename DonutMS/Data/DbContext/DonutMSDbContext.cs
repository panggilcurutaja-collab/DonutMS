using Microsoft.EntityFrameworkCore;
using DonutMS.Data.Entities;

namespace DonutMS.Data.DbContext;

public class DonutMSDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DonutMSDbContext(DbContextOptions<DonutMSDbContext> options) : base(options) { }

    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<IngredientPrice> IngredientPrices => Set<IngredientPrice>();
    public DbSet<IngredientAllergen> IngredientAllergens => Set<IngredientAllergen>();

    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeVersion> RecipeVersions => Set<RecipeVersion>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeVersionIngredient> RecipeVersionIngredients => Set<RecipeVersionIngredient>();
    public DbSet<RecipeSubstitution> RecipeSubstitutions => Set<RecipeSubstitution>();

    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchIngredient> BatchIngredients => Set<BatchIngredient>();
    public DbSet<BatchLabor> BatchLabors => Set<BatchLabor>();
    public DbSet<QualityControl> QualityControls => Set<QualityControl>();

    public DbSet<InventoryStock> InventoryStocks => Set<InventoryStock>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<InventoryAging> InventoryAgings => Set<InventoryAging>();

    public DbSet<SKU> SKUs => Set<SKU>();
    public DbSet<SKUCost> SKUCosts => Set<SKUCost>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<OperatingCost> OperatingCosts => Set<OperatingCost>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<BundlePackage> BundlePackages => Set<BundlePackage>();

    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<LaborRate> LaborRates => Set<LaborRate>();
    public DbSet<EquipmentDepreciation> EquipmentDepreciations => Set<EquipmentDepreciation>();
    public DbSet<UtilityExpense> UtilityExpenses => Set<UtilityExpense>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<PurchaseOrderReceiving> PurchaseOrderReceivings => Set<PurchaseOrderReceiving>();
    public DbSet<PurchaseOrderReceivingDetail> PurchaseOrderReceivingDetails => Set<PurchaseOrderReceivingDetail>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Allergen> Allergens => Set<Allergen>();
    public DbSet<SKUAllergen> SKUAllergens => Set<SKUAllergen>();
    public DbSet<NutritionalInfo> NutritionalInfos => Set<NutritionalInfo>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyEntityConfigurations(modelBuilder);
        ApplyRelationshipConfigurations(modelBuilder);
        ApplyIndexes(modelBuilder);
    }

    private void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Unit>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(150);
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Ingredient>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SKU).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.SKU).HasMaxLength(50);
        });

        modelBuilder.Entity<Recipe>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Batch>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.BatchCode).IsUnique();
            e.Property(x => x.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<SKU>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50);
        });

        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PONumber).IsUnique();
            e.Property(x => x.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EntityName, x.EntityId });
            e.HasIndex(x => x.AuditDate);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100);
        });
    }

    private void ApplyRelationshipConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>()
            .HasOne(x => x.ConsumptionUnit)
            .WithMany()
            .HasForeignKey(x => x.ConsumptionUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ingredient>()
            .HasOne(x => x.PurchaseUnit)
            .WithMany()
            .HasForeignKey(x => x.PurchaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IngredientPrice>()
            .HasOne(x => x.Ingredient)
            .WithMany(x => x.Prices)
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IngredientPrice>()
            .HasOne(x => x.Supplier)
            .WithMany(x => x.IngredientPrices)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IngredientPrice>()
            .HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Recipe>()
            .HasOne(x => x.YieldUnit)
            .WithMany()
            .HasForeignKey(x => x.YieldUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeVersion>()
            .HasOne(x => x.Recipe)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeVersion>()
            .HasOne(x => x.YieldUnit)
            .WithMany()
            .HasForeignKey(x => x.YieldUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(x => x.Recipe)
            .WithMany(x => x.RecipeIngredients)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(x => x.Ingredient)
            .WithMany(x => x.RecipeIngredients)
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeVersionIngredient>()
            .HasOne(x => x.RecipeVersion)
            .WithMany(x => x.Ingredients)
            .HasForeignKey(x => x.RecipeVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeVersionIngredient>()
            .HasOne(x => x.Ingredient)
            .WithMany()
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeVersionIngredient>()
            .HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeSubstitution>()
            .HasOne(x => x.OriginalIngredient)
            .WithMany()
            .HasForeignKey(x => x.OriginalIngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeSubstitution>()
            .HasOne(x => x.SubstituteIngredient)
            .WithMany()
            .HasForeignKey(x => x.SubstituteIngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Batch>()
            .HasOne(x => x.Recipe)
            .WithMany(x => x.Batches)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Batch>()
            .HasOne(x => x.RecipeVersion)
            .WithMany()
            .HasForeignKey(x => x.RecipeVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Batch>()
            .HasOne(x => x.YieldUnit)
            .WithMany()
            .HasForeignKey(x => x.YieldUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatchIngredient>()
            .HasOne(x => x.Batch)
            .WithMany(x => x.Ingredients)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BatchIngredient>()
            .HasOne(x => x.Ingredient)
            .WithMany(x => x.BatchIngredients)
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatchIngredient>()
            .HasOne(x => x.PlannedUnit)
            .WithMany()
            .HasForeignKey(x => x.PlannedUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatchIngredient>()
            .HasOne(x => x.StockBatch)
            .WithMany(x => x.BatchIngredients)
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<QualityControl>()
            .HasOne(x => x.Batch)
            .WithMany(x => x.QualityControls)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BatchLabor>()
            .HasOne(x => x.Batch)
            .WithMany(x => x.LaborRecords)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BatchLabor>()
            .HasOne(x => x.Operator)
            .WithMany(x => x.BatchLaborRecords)
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryStock>()
            .HasOne(x => x.Ingredient)
            .WithMany(x => x.InventoryStocks)
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryStock>()
            .HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockBatch>()
            .HasOne(x => x.InventoryStock)
            .WithMany(x => x.StockBatches)
            .HasForeignKey(x => x.InventoryStockId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockTransaction>()
            .HasOne(x => x.InventoryStock)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.InventoryStockId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockTransaction>()
            .HasOne(x => x.StockBatch)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockTransaction>()
            .HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransaction>()
            .HasOne(x => x.Batch)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockTransaction>()
            .HasOne(x => x.PurchaseOrder)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryAging>()
            .HasOne(x => x.InventoryStock)
            .WithMany()
            .HasForeignKey(x => x.InventoryStockId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SKU>()
            .HasOne(x => x.Recipe)
            .WithMany(x => x.SKUs)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SKUCost>()
            .HasOne(x => x.SKU)
            .WithMany(x => x.Costs)
            .HasForeignKey(x => x.SKUId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceHistory>()
            .HasOne(x => x.SKU)
            .WithMany(x => x.PriceHistory)
            .HasForeignKey(x => x.SKUId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BundlePackage>()
            .HasOne(x => x.SKU)
            .WithMany()
            .HasForeignKey(x => x.SKUId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LaborRate>()
            .HasOne(x => x.Operator)
            .WithMany(x => x.LaborRates)
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(x => x.Supplier)
            .WithMany(x => x.PurchaseOrders)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(x => x.Ingredient)
            .WithMany()
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderReceiving>()
            .HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Receivings)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderReceivingDetail>()
            .HasOne(x => x.Receiving)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.ReceivingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderReceivingDetail>()
            .HasOne(x => x.PurchaseOrderItem)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SKUAllergen>()
            .HasOne(x => x.SKU)
            .WithMany(x => x.Allergens)
            .HasForeignKey(x => x.SKUId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SKUAllergen>()
            .HasOne(x => x.Allergen)
            .WithMany(x => x.SKUAllergens)
            .HasForeignKey(x => x.AllergenId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IngredientAllergen>()
            .HasOne(x => x.Ingredient)
            .WithMany(x => x.Allergens)
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IngredientAllergen>()
            .HasOne(x => x.Allergen)
            .WithMany()
            .HasForeignKey(x => x.AllergenId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NutritionalInfo>()
            .HasOne(x => x.SKU)
            .WithMany()
            .HasForeignKey(x => x.SKUId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void ApplyIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IngredientPrice>()
            .HasIndex(x => new { x.IngredientId, x.SupplierId, x.EffectiveDate });

        modelBuilder.Entity<Batch>()
            .HasIndex(x => x.ProductionDate);

        modelBuilder.Entity<StockTransaction>()
            .HasIndex(x => x.TransactionDate);

        modelBuilder.Entity<RecipeVersion>()
            .HasIndex(x => new { x.RecipeId, x.VersionNumber });

        modelBuilder.Entity<SKUCost>()
            .HasIndex(x => new { x.SKUId, x.EffectiveDate });

        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(x => new { x.SupplierId, x.OrderDate });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseModel && e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
                ((BaseModel)entry.Entity).CreatedAt = DateTime.UtcNow;

            if (entry.State == EntityState.Modified)
                ((BaseModel)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }
    }
}

public class BaseModel
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
