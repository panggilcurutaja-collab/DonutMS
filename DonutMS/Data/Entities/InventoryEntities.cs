using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DonutMS.Core.MVVM;

namespace DonutMS.Data.Entities;

[Table("InventoryStocks")]
public class InventoryStock : BaseModel
{
    [ForeignKey("Ingredient")]
    public int IngredientId { get; set; }

    public decimal Quantity { get; set; }

    [ForeignKey("Unit")]
    public int UnitId { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal AvailableQuantity => Quantity - ReservedQuantity;

    public DateTime LastUpdated { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public Ingredient? Ingredient { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<StockBatch>? StockBatches { get; set; }
    public ICollection<StockTransaction>? Transactions { get; set; }
}

[Table("StockBatches")]
public class StockBatch : BaseModel
{
    [ForeignKey("InventoryStock")]
    public int InventoryStockId { get; set; }

    [StringLength(100)]
    public string? SupplierBatchNumber { get; set; }

    public DateTime ReceiptDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public decimal QuantityReceived { get; set; }

    public decimal QuantityUsed { get; set; }

    public decimal QuantityWasted { get; set; }

    public decimal AvailableQuantity => QuantityReceived - QuantityUsed - QuantityWasted;

    [StringLength(50)]
    public string Status { get; set; } = "Active";

    public int DaysToExpiry => ExpiryDate.HasValue ? (int)(ExpiryDate.Value.Date - DateTime.Today).TotalDays : int.MaxValue;

    [StringLength(500)]
    public string? Notes { get; set; }

    public InventoryStock? InventoryStock { get; set; }
    public ICollection<BatchIngredient>? BatchIngredients { get; set; }
    public ICollection<StockTransaction>? Transactions { get; set; }
}

[Table("StockTransactions")]
public class StockTransaction : BaseModel
{
    [ForeignKey("InventoryStock")]
    public int InventoryStockId { get; set; }

    [ForeignKey("StockBatch")]
    public int? StockBatchId { get; set; }

    [StringLength(50)]
    public required string TransactionType { get; set; }

    public decimal Quantity { get; set; }

    [ForeignKey("Unit")]
    public int UnitId { get; set; }

    public DateTime TransactionDate { get; set; }

    [ForeignKey("Batch")]
    public int? BatchId { get; set; }

    [ForeignKey("PurchaseOrder")]
    public int? PurchaseOrderId { get; set; }

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public InventoryStock? InventoryStock { get; set; }
    public StockBatch? StockBatch { get; set; }
    public Unit? Unit { get; set; }
    public Batch? Batch { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}

[Table("InventoryAging")]
public class InventoryAging : BaseModel
{
    [ForeignKey("InventoryStock")]
    public int InventoryStockId { get; set; }

    public int DaysInStock { get; set; }

    public decimal Quantity { get; set; }

    public DateTime CalculatedDate { get; set; }

    [StringLength(50)]
    public string AgeCategory { get; set; } = "Current";

    public InventoryStock? InventoryStock { get; set; }
}
