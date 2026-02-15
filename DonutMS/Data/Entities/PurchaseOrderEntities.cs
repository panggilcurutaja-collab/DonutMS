using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DonutMS.Core.Domain;
using DonutMS.Core.MVVM;

namespace DonutMS.Data.Entities;

[Table("PurchaseOrders")]
public class PurchaseOrder : BaseModel
{
    [Required]
    [StringLength(50)]
    public required string PONumber { get; set; }

    [ForeignKey("Supplier")]
    public int SupplierId { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime RequiredDeliveryDate { get; set; }

    public DateTime? ActualDeliveryDate { get; set; }

    public decimal TotalAmount { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = DomainConstants.PurchaseOrderStatus.Draft;

    [StringLength(50)]
    public string? PaymentStatus { get; set; } = "Unpaid";

    [StringLength(500)]
    public string? DeliveryAddress { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<PurchaseOrderReceiving> Receivings { get; set; } = new List<PurchaseOrderReceiving>();
}

[Table("PurchaseOrderItems")]
public class PurchaseOrderItem : BaseModel
{
    [ForeignKey("PurchaseOrder")]
    public int PurchaseOrderId { get; set; }

    [ForeignKey("Ingredient")]
    public int IngredientId { get; set; }

    public int LineNumber { get; set; }

    public decimal OrderedQuantity { get; set; }

    [ForeignKey("Unit")]
    public int UnitId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public decimal? ReceivedQuantity { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = DomainConstants.PurchaseOrderItemStatus.Pending;

    [StringLength(500)]
    public string? Notes { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Ingredient Ingredient { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}

[Table("PurchaseOrderReceivings")]
public class PurchaseOrderReceiving : BaseModel
{
    [ForeignKey("PurchaseOrder")]
    public int PurchaseOrderId { get; set; }

    public DateTime ReceivingDate { get; set; }

    [StringLength(100)]
    public string? ReceivedBy { get; set; }

    public decimal TotalReceivedQuantity { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public ICollection<PurchaseOrderReceivingDetail> Details { get; set; } = new List<PurchaseOrderReceivingDetail>();
}

[Table("PurchaseOrderReceivingDetails")]
public class PurchaseOrderReceivingDetail : BaseModel
{
    [ForeignKey("Receiving")]
    public int ReceivingId { get; set; }

    [ForeignKey("PurchaseOrderItem")]
    public int PurchaseOrderItemId { get; set; }

    public decimal ReceivedQuantity { get; set; }

    [StringLength(100)]
    public string? SupplierBatchNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public PurchaseOrderReceiving Receiving { get; set; } = null!;
    public PurchaseOrderItem PurchaseOrderItem { get; set; } = null!;
}
