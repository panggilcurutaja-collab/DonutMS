using AutoMapper;
using DonutMS.Data.Entities;

namespace DonutMS.Models.DTOs;

// ========== RECIPE DTOs ==========
public class RecipeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal YieldPerBatch { get; set; }
    public int YieldUnitId { get; set; }
    public string YieldUnitCode { get; set; } = string.Empty;
    public decimal EstimatedProductionTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public IEnumerable<RecipeIngredientDto> Ingredients { get; set; } = new List<RecipeIngredientDto>();
}

public class CreateRecipeDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal YieldPerBatch { get; set; }
    public int YieldUnitId { get; set; }
    public decimal EstimatedProductionTime { get; set; } = 0;
}

public class UpdateRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? EstimatedProductionTime { get; set; }
    public bool? IsActive { get; set; }
}

public class RecipeVersionDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int VersionNumber { get; set; }
    public string? ChangeNotes { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ObsoleteDate { get; set; }
    public bool IsActive { get; set; }
    public decimal YieldPerBatch { get; set; }
    public int YieldUnitId { get; set; }
    public IEnumerable<RecipeVersionIngredientDto> Ingredients { get; set; } = new List<RecipeVersionIngredientDto>();
}

public class CreateRecipeVersionDto
{
    public string? ChangeNotes { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal YieldPerBatch { get; set; }
    public int YieldUnitId { get; set; }
}

public class RecipeIngredientDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityPerBatch { get; set; }
    public int UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsOptional { get; set; }
}

public class AddRecipeIngredientDto
{
    public int IngredientId { get; set; }
    public decimal QuantityPerBatch { get; set; }
    public int UnitId { get; set; }
}

public class UpdateRecipeIngredientDto
{
    public decimal QuantityPerBatch { get; set; }
    public int UnitId { get; set; }
}

public class RecipeVersionIngredientDto
{
    public int Id { get; set; }
    public int RecipeVersionId { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityPerBatch { get; set; }
    public int UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal WastePercentage { get; set; }
}

public class RecipeSubstitutionDto
{
    public int Id { get; set; }
    public int OriginalIngredientId { get; set; }
    public string OriginalIngredientName { get; set; } = string.Empty;
    public int SubstituteIngredientId { get; set; }
    public string SubstituteIngredientName { get; set; } = string.Empty;
    public decimal SubstitutionRatio { get; set; }
    public decimal CostImpact { get; set; }
    public bool IsApproved { get; set; }
}

public class CreateRecipeSubstitutionDto
{
    public int OriginalIngredientId { get; set; }
    public int SubstituteIngredientId { get; set; }
    public decimal SubstitutionRatio { get; set; }
    public decimal CostImpact { get; set; }
}

// ========== INGREDIENT DTOs ==========
public class IngredientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ConsumptionUnitId { get; set; }
    public string ConsumptionUnitCode { get; set; } = string.Empty;
    public int PurchaseUnitId { get; set; }
    public string PurchaseUnitCode { get; set; } = string.Empty;
    public decimal MinimumStockLevel { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal ReorderQuantity { get; set; }
    public int ShelfLifeDays { get; set; }
    public bool IsActive { get; set; }
    public IngredientPriceDto? CurrentPrice { get; set; }
}

public class CreateIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ConsumptionUnitId { get; set; }
    public int PurchaseUnitId { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal ReorderQuantity { get; set; }
    public int ShelfLifeDays { get; set; }
}

public class UpdateIngredientDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? ConsumptionUnitId { get; set; }
    public int? PurchaseUnitId { get; set; }
    public decimal? MinimumStockLevel { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? ReorderQuantity { get; set; }
    public int? ShelfLifeDays { get; set; }
    public bool? IsActive { get; set; }
}

public class UnitDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public string? BaseUnit { get; set; }
    public bool IsActive { get; set; }
}

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public int LeadTimeDays { get; set; }
    public string? PaymentTerms { get; set; }
    public bool IsActive { get; set; }
}

public class IngredientPriceDto
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

// ========== INVENTORY DTOs ==========
public class InventoryStockDto
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public DateTime LastUpdated { get; set; }
    public IEnumerable<StockBatchDto> Batches { get; set; } = new List<StockBatchDto>();
}

public class StockBatchDto
{
    public int Id { get; set; }
    public string? SupplierBatchNumber { get; set; }
    public DateTime ReceiptDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityUsed { get; set; }
    public decimal QuantityWasted { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysToExpiry { get; set; }
}

public class StockTransactionDto
{
    public int Id { get; set; }
    public int InventoryStockId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public int? BatchId { get; set; }
    public string? ReferenceNumber { get; set; }
}

// ========== BATCH / PRODUCTION DTOs ==========
public class BatchDto
{
    public int Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public DateTime ProductionDate { get; set; }
    public decimal TargetYield { get; set; }
    public decimal? ActualYield { get; set; }
    public decimal? WasteQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasQCPass { get; set; }
    public IEnumerable<BatchIngredientDto> Ingredients { get; set; } = new List<BatchIngredientDto>();
    public IEnumerable<QualityControlDto> QualityControls { get; set; } = new List<QualityControlDto>();
}

public class CreateBatchDto
{
    public string BatchCode { get; set; } = string.Empty;
    public int RecipeId { get; set; }
    public DateTime ProductionDate { get; set; }
    public decimal TargetYield { get; set; }
}

public class BatchIngredientDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public int PlannedUnitId { get; set; }
    public string PlannedUnitCode { get; set; } = string.Empty;
    public decimal? ActualQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class QualityControlDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public DateTime InspectionDate { get; set; }
    public string? InspectedBy { get; set; }
    public bool Passed { get; set; }
    public decimal? Taste { get; set; }
    public decimal? Texture { get; set; }
    public decimal? Appearance { get; set; }
    public decimal? Aroma { get; set; }
    public decimal? AverageScore { get; set; }
}

// ========== SKU / COSTING DTOs ==========
public class SKUDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal RetailPrice { get; set; }
    public SKUCostDto? CurrentCost { get; set; }
    public decimal? HPP { get; set; }
    public decimal? GrossMargin { get; set; }
}

public class CreateSKUDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int? RecipeId { get; set; }
    public decimal RetailPrice { get; set; }
}

public class SKUCostDto
{
    public int Id { get; set; }
    public int SKUId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalHPP { get; set; }
    public decimal GrossMargin { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSKUCostDto
{
    public int SKUId { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
}

// ========== PURCHASE ORDER DTOs ==========
public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime RequiredDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public IEnumerable<PurchaseOrderItemDto> Items { get; set; } = new List<PurchaseOrderItemDto>();
}

public class CreatePurchaseOrderDto
{
    public string PONumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public DateTime RequiredDeliveryDate { get; set; }
    public IEnumerable<CreatePurchaseOrderItemDto> Items { get; set; } = new List<CreatePurchaseOrderItemDto>();
}

public class PurchaseOrderItemDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public decimal OrderedQuantity { get; set; }
    public int UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal? ReceivedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreatePurchaseOrderItemDto
{
    public int IngredientId { get; set; }
    public decimal OrderedQuantity { get; set; }
    public int UnitId { get; set; }
    public decimal UnitPrice { get; set; }
}

// ========== AUTOMAP PROFILE ==========
public class DtoMappingProfile : Profile
{
    public DtoMappingProfile()
    {
        // Recipe Mappings
        CreateMap<Recipe, RecipeDto>()
            .ForMember(d => d.YieldUnitCode, o => o.MapFrom(s => s.YieldUnit != null ? s.YieldUnit.Code : ""))
            .ForMember(d => d.Ingredients, o => o.MapFrom(s => s.RecipeIngredients));

        CreateMap<CreateRecipeDto, Recipe>();
        CreateMap<UpdateRecipeDto, Recipe>();

        CreateMap<RecipeVersion, RecipeVersionDto>();
        CreateMap<CreateRecipeVersionDto, RecipeVersion>();

        CreateMap<RecipeIngredient, RecipeIngredientDto>()
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.Name : ""))
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""));

        CreateMap<AddRecipeIngredientDto, RecipeIngredient>();
        CreateMap<UpdateRecipeIngredientDto, RecipeIngredient>();

        CreateMap<RecipeVersionIngredient, RecipeVersionIngredientDto>()
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.Name : ""))
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""));

        CreateMap<RecipeSubstitution, RecipeSubstitutionDto>()
            .ForMember(d => d.OriginalIngredientName, o => o.MapFrom(s => s.OriginalIngredient != null ? s.OriginalIngredient.Name : ""))
            .ForMember(d => d.SubstituteIngredientName, o => o.MapFrom(s => s.SubstituteIngredient != null ? s.SubstituteIngredient.Name : ""));

        CreateMap<CreateRecipeSubstitutionDto, RecipeSubstitution>();

        // Ingredient Mappings
        CreateMap<Ingredient, IngredientDto>()
            .ForMember(d => d.ConsumptionUnitCode, o => o.MapFrom(s => s.ConsumptionUnit != null ? s.ConsumptionUnit.Code : ""))
            .ForMember(d => d.PurchaseUnitCode, o => o.MapFrom(s => s.PurchaseUnit != null ? s.PurchaseUnit.Code : ""));

        CreateMap<CreateIngredientDto, Ingredient>();

        CreateMap<IngredientPrice, IngredientPriceDto>()
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.Name : ""))
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""));

        // Inventory Mappings
        CreateMap<InventoryStock, InventoryStockDto>()
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.Name : ""))
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""))
            .ForMember(d => d.Batches, o => o.MapFrom(s => s.StockBatches));

        CreateMap<StockBatch, StockBatchDto>();

        CreateMap<StockTransaction, StockTransactionDto>()
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""));

        // Batch/Production Mappings
        CreateMap<Batch, BatchDto>()
            .ForMember(d => d.RecipeName, o => o.MapFrom(s => s.Recipe != null ? s.Recipe.Name : ""));

        CreateMap<CreateBatchDto, Batch>();

        CreateMap<BatchIngredient, BatchIngredientDto>()
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.Name : ""))
            .ForMember(d => d.PlannedUnitCode, o => o.MapFrom(s => s.PlannedUnit != null ? s.PlannedUnit.Code : ""));

        CreateMap<QualityControl, QualityControlDto>();

        // SKU/Costing Mappings
        CreateMap<SKU, SKUDto>();
        CreateMap<CreateSKUDto, SKU>();

        CreateMap<SKUCost, SKUCostDto>();
        CreateMap<CreateSKUCostDto, SKUCost>();

        // Purchase Order Mappings
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.Name : ""));

        CreateMap<CreatePurchaseOrderDto, PurchaseOrder>();

        CreateMap<PurchaseOrderItem, PurchaseOrderItemDto>()
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.Name : ""))
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""));

        CreateMap<CreatePurchaseOrderItemDto, PurchaseOrderItem>();

        // Unit Mappings
        CreateMap<Unit, UnitDto>();

        // Supplier Mappings
        CreateMap<Supplier, SupplierDto>();
    }
}
