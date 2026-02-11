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
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal YieldPerBatch { get; set; }
    public int YieldUnitId { get; set; }
    public decimal EstimatedProductionTime { get; set; }
    public bool IsActive { get; set; } = true;
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

// ========== COSTING DTOs ==========
public class IngredientCostLineDto
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityPerBatch { get; set; }
    public int UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal? PricePerUnit { get; set; }
    public int? PriceUnitId { get; set; }
    public string? PriceUnitCode { get; set; }
    public decimal Cost { get; set; }
    public decimal CostWithWaste { get; set; }
    public bool HasPrice { get; set; }
    public string? Error { get; set; }
}

public class RecipeCostBreakdownDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public decimal YieldPerBatch { get; set; }
    public decimal WastePercent { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal HppPerUnit { get; set; }
    public IEnumerable<IngredientCostLineDto> IngredientCosts { get; set; } = new List<IngredientCostLineDto>();
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
    public string? IngredientSKU { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public DateTime LastUpdated { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal ReorderPoint { get; set; }
    public int ShelfLifeDays { get; set; }
    public DateTime? NextExpiryDate { get; set; }
    public int? NextExpiryDays { get; set; }
    public bool IsLowStock { get; set; }
    public IEnumerable<StockBatchDto> Batches { get; set; } = new List<StockBatchDto>();
    public IEnumerable<StockTransactionDto> Transactions { get; set; } = new List<StockTransactionDto>();
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
    public string IngredientName { get; set; } = string.Empty;
    public string? StockBatchNumber { get; set; }
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
    public string YieldUnitCode { get; set; } = string.Empty;
    public decimal? ActualYield { get; set; }
    public decimal? WasteQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasQCPass { get; set; }
    public string? Notes { get; set; }
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
    public string? DefectsFound { get; set; }
    public string? Remarks { get; set; }
}

// ========== SKU / COSTING DTOs ==========
public class SKUDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public string? Category { get; set; }
    public decimal RetailPrice { get; set; }
    public bool IsActive { get; set; }
    public SKUCostDto? CurrentCost { get; set; }
    public decimal? HPP { get; set; }
    public decimal? GrossMargin { get; set; }
}

public class CreateSKUDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? RecipeId { get; set; }
    public decimal RetailPrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSKUDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? RecipeId { get; set; }
    public decimal? RetailPrice { get; set; }
    public bool? IsActive { get; set; }
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

public class AllergenDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
}

public class SKUAllergenDto
{
    public int Id { get; set; }
    public int SKUId { get; set; }
    public int AllergenId { get; set; }
    public string? AllergenName { get; set; }
    public bool MayContainTrace { get; set; }
    public string? Notes { get; set; }
}

public class NutritionalInfoDto
{
    public int Id { get; set; }
    public int SKUId { get; set; }
    public decimal Calories { get; set; }
    public decimal Protein { get; set; }
    public decimal Fat { get; set; }
    public decimal Carbohydrates { get; set; }
    public decimal Fiber { get; set; }
    public decimal Sugar { get; set; }
    public decimal Sodium { get; set; }
    public string? ServingSize { get; set; }
    public int? ServingsPerPackage { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
}

public class UpsertNutritionalInfoDto
{
    public decimal Calories { get; set; }
    public decimal Protein { get; set; }
    public decimal Fat { get; set; }
    public decimal Carbohydrates { get; set; }
    public decimal Fiber { get; set; }
    public decimal Sugar { get; set; }
    public decimal Sodium { get; set; }
    public string? ServingSize { get; set; }
    public int? ServingsPerPackage { get; set; }
    public string? Notes { get; set; }
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
    public string? PaymentStatus { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<PurchaseOrderItemDto> Items { get; set; } = new List<PurchaseOrderItemDto>();
    public IEnumerable<PurchaseOrderReceivingDto> Receivings { get; set; } = new List<PurchaseOrderReceivingDto>();
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

public class PurchaseOrderReceivingDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public DateTime ReceivingDate { get; set; }
    public string? ReceivedBy { get; set; }
    public decimal TotalReceivedQuantity { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<PurchaseOrderReceivingDetailDto> Details { get; set; } = new List<PurchaseOrderReceivingDetailDto>();
}

public class PurchaseOrderReceivingDetailDto
{
    public int Id { get; set; }
    public int ReceivingId { get; set; }
    public int PurchaseOrderItemId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string? SupplierBatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public string? IngredientName { get; set; }
    public string? UnitCode { get; set; }
}

public class CreatePurchaseOrderReceivingDto
{
    public int PurchaseOrderId { get; set; }
    public DateTime ReceivingDate { get; set; }
    public string? ReceivedBy { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<CreatePurchaseOrderReceivingDetailDto> Details { get; set; } = new List<CreatePurchaseOrderReceivingDetailDto>();
}

public class CreatePurchaseOrderReceivingDetailDto
{
    public int PurchaseOrderItemId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string? SupplierBatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

// ========== PROMO / DISCOUNT DTOs ==========
public class DiscountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ApplicableFor { get; set; }
    public int? MinimumQuantity { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class CreateDiscountDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);
    public string? ApplicableFor { get; set; }
    public int? MinimumQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public class BundlePackageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BundlePrice { get; set; }
    public int Quantity { get; set; }
    public int? SKUId { get; set; }
    public string? SKUName { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class CreateBundlePackageDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BundlePrice { get; set; }
    public int Quantity { get; set; }
    public int? SKUId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

// ========== LABOR & OVERHEAD DTOs ==========
public class OperatorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? JobTitle { get; set; }
    public decimal BaseSalary { get; set; }
    public string SalaryPeriod { get; set; } = "Monthly";
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class CreateOperatorDto
{
    public string Name { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public DateTime? TerminationDate { get; set; }
    public string? JobTitle { get; set; }
    public decimal BaseSalary { get; set; }
    public string SalaryPeriod { get; set; } = "Monthly";
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public class UpdateOperatorDto
{
    public string? Name { get; set; }
    public string? EmployeeId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? JobTitle { get; set; }
    public decimal? BaseSalary { get; set; }
    public string? SalaryPeriod { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class LaborRateDto
{
    public int Id { get; set; }
    public int OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public decimal OvertimeMultiplier { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ShiftType { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class CreateLaborRateDto
{
    public int OperatorId { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal OvertimeMultiplier { get; set; } = 1.5m;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string? ShiftType { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BatchLaborDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public int OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; }
    public string? ShiftType { get; set; }
    public string? Notes { get; set; }
}

public class CreateBatchLaborDto
{
    public int BatchId { get; set; }
    public int OperatorId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; }
    public string? ShiftType { get; set; }
    public string? Notes { get; set; }
}

public class EquipmentDepreciationDto
{
    public int Id { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal AcquisitionCost { get; set; }
    public DateTime AcquisitionDate { get; set; }
    public int DepreciationYears { get; set; }
    public string DepreciationMethod { get; set; } = "StraightLine";
    public decimal ResidualValue { get; set; }
    public decimal MonthlyDepreciation { get; set; }
    public DateTime? DisposalDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class UtilityExpenseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UtilityType { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string AllocationMethod { get; set; } = "Percentage";
    public decimal AllocationValue { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
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
            .ForMember(d => d.PurchaseUnitCode, o => o.MapFrom(s => s.PurchaseUnit != null ? s.PurchaseUnit.Code : ""))
            .ForMember(d => d.CurrentPrice, o => o.MapFrom(s =>
                s.Prices != null
                    ? s.Prices
                        .Where(p => p.IsActive && p.EffectiveDate <= DateTime.UtcNow && (!p.EndDate.HasValue || p.EndDate >= DateTime.UtcNow))
                        .OrderByDescending(p => p.EffectiveDate)
                        .FirstOrDefault()
                    : null));

        CreateMap<CreateIngredientDto, Ingredient>();

        CreateMap<IngredientPrice, IngredientPriceDto>()
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.Name : ""))
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""));

        // Inventory Mappings
        CreateMap<InventoryStock, InventoryStockDto>()
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.Name : ""))
            .ForMember(d => d.IngredientSKU, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.SKU : null))
            .ForMember(d => d.MinimumStockLevel, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.MinimumStockLevel : 0))
            .ForMember(d => d.ReorderPoint, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.ReorderPoint : 0))
            .ForMember(d => d.ShelfLifeDays, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.ShelfLifeDays : 0))
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""))
            .ForMember(d => d.Batches, o => o.MapFrom(s => s.StockBatches))
            .ForMember(d => d.Transactions, o => o.MapFrom(s => s.Transactions));

        CreateMap<StockBatch, StockBatchDto>();

        CreateMap<StockTransaction, StockTransactionDto>()
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""))
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.InventoryStock != null && s.InventoryStock.Ingredient != null ? s.InventoryStock.Ingredient.Name : ""))
            .ForMember(d => d.StockBatchNumber, o => o.MapFrom(s => s.StockBatch != null ? s.StockBatch.SupplierBatchNumber : null));

        // Batch/Production Mappings
        CreateMap<Batch, BatchDto>()
            .ForMember(d => d.RecipeName, o => o.MapFrom(s => s.Recipe != null ? s.Recipe.Name : ""))
            .ForMember(d => d.YieldUnitCode, o => o.MapFrom(s => s.YieldUnit != null ? s.YieldUnit.Code : ""));

        CreateMap<CreateBatchDto, Batch>();

        CreateMap<BatchIngredient, BatchIngredientDto>()
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.Name : ""))
            .ForMember(d => d.PlannedUnitCode, o => o.MapFrom(s => s.PlannedUnit != null ? s.PlannedUnit.Code : ""));

        CreateMap<QualityControl, QualityControlDto>();

        // SKU/Costing Mappings
        CreateMap<SKU, SKUDto>()
            .ForMember(d => d.RecipeName, o => o.MapFrom(s => s.Recipe != null ? s.Recipe.Name : ""));
        CreateMap<CreateSKUDto, SKU>();
        CreateMap<UpdateSKUDto, SKU>();

        CreateMap<SKUCost, SKUCostDto>();
        CreateMap<CreateSKUCostDto, SKUCost>();

        CreateMap<Allergen, AllergenDto>();

        CreateMap<SKUAllergen, SKUAllergenDto>()
            .ForMember(d => d.AllergenName, o => o.MapFrom(s => s.Allergen != null ? s.Allergen.Name : ""));

        CreateMap<NutritionalInfo, NutritionalInfoDto>();
        CreateMap<UpsertNutritionalInfoDto, NutritionalInfo>();

        // Purchase Order Mappings
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.Name : ""));

        CreateMap<CreatePurchaseOrderDto, PurchaseOrder>();

        CreateMap<PurchaseOrderItem, PurchaseOrderItemDto>()
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.Ingredient != null ? s.Ingredient.Name : ""))
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.Unit != null ? s.Unit.Code : ""));

        CreateMap<CreatePurchaseOrderItemDto, PurchaseOrderItem>();

        CreateMap<PurchaseOrderReceiving, PurchaseOrderReceivingDto>();
        CreateMap<CreatePurchaseOrderReceivingDto, PurchaseOrderReceiving>();

        CreateMap<PurchaseOrderReceivingDetail, PurchaseOrderReceivingDetailDto>()
            .ForMember(d => d.IngredientName, o => o.MapFrom(s => s.PurchaseOrderItem != null && s.PurchaseOrderItem.Ingredient != null ? s.PurchaseOrderItem.Ingredient.Name : ""))
            .ForMember(d => d.UnitCode, o => o.MapFrom(s => s.PurchaseOrderItem != null && s.PurchaseOrderItem.Unit != null ? s.PurchaseOrderItem.Unit.Code : ""));

        CreateMap<CreatePurchaseOrderReceivingDetailDto, PurchaseOrderReceivingDetail>();

        // Unit Mappings
        CreateMap<Unit, UnitDto>();

        // Supplier Mappings
        CreateMap<Supplier, SupplierDto>();

        // Labor & Overhead Mappings
        CreateMap<Operator, OperatorDto>();
        CreateMap<CreateOperatorDto, Operator>();
        CreateMap<UpdateOperatorDto, Operator>();

        CreateMap<LaborRate, LaborRateDto>()
            .ForMember(d => d.OperatorName, o => o.MapFrom(s => s.Operator != null ? s.Operator.Name : ""));
        CreateMap<CreateLaborRateDto, LaborRate>();

        CreateMap<BatchLabor, BatchLaborDto>()
            .ForMember(d => d.OperatorName, o => o.MapFrom(s => s.Operator != null ? s.Operator.Name : ""))
            .ForMember(d => d.BatchCode, o => o.MapFrom(s => s.Batch != null ? s.Batch.BatchCode : ""));
        CreateMap<CreateBatchLaborDto, BatchLabor>();

        CreateMap<EquipmentDepreciation, EquipmentDepreciationDto>();
        CreateMap<EquipmentDepreciationDto, EquipmentDepreciation>();

        CreateMap<UtilityExpense, UtilityExpenseDto>();
        CreateMap<UtilityExpenseDto, UtilityExpense>();

        // Promo / Discount Mappings
        CreateMap<Discount, DiscountDto>();
        CreateMap<CreateDiscountDto, Discount>();

        CreateMap<BundlePackage, BundlePackageDto>()
            .ForMember(d => d.SKUName, o => o.MapFrom(s => s.SKU != null ? s.SKU.Name : null));
        CreateMap<CreateBundlePackageDto, BundlePackage>();
    }
}
