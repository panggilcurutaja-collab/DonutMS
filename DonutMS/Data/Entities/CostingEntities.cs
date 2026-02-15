using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DonutMS.Core.MVVM;

namespace DonutMS.Data.Entities;

[Table("SKUs")]
public class SKU : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [StringLength(50)]
    public required string Code { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [ForeignKey("Recipe")]
    public int? RecipeId { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    public decimal RetailPrice { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    public Recipe? Recipe { get; set; }
    public ICollection<SKUCost> Costs { get; set; } = new List<SKUCost>();
    public ICollection<PriceHistory> PriceHistory { get; set; } = new List<PriceHistory>();
    public ICollection<SKUAllergen> Allergens { get; set; } = new List<SKUAllergen>();
}

[Table("SKUCosts")]
public class SKUCost : BaseModel
{
    [ForeignKey("SKU")]
    public int SKUId { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }

    public decimal MaterialCost { get; set; }

    public decimal PackagingCost { get; set; }

    public decimal LaborCost { get; set; }

    public decimal OverheadCost { get; set; }

    public decimal TotalHPP { get; set; }

    public decimal GrossMargin { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    public SKU SKU { get; set; } = null!;
}

[Table("PriceHistories")]
public class PriceHistory : BaseModel
{
    [ForeignKey("SKU")]
    public int SKUId { get; set; }

    public decimal OldPrice { get; set; }

    public decimal NewPrice { get; set; }

    public DateTime EffectiveDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(100)]
    public string? ChangedBy { get; set; }

    public SKU SKU { get; set; } = null!;
}

[Table("OperatingCosts")]
public class OperatingCost : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string CostType { get; set; } = "Fixed";

    public decimal Amount { get; set; }

    [StringLength(50)]
    public string? Period { get; set; } = "Monthly";

    public DateTime EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(50)]
    public string AllocationMethod { get; set; } = "Percentage";

    public decimal AllocationValue { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }
}

[Table("Discounts")]
public class Discount : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string DiscountType { get; set; } = "Percentage";

    public decimal DiscountValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [StringLength(50)]
    public string? ApplicableFor { get; set; }

    public int? MinimumQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }
}

[Table("BundlePackages")]
public class BundlePackage : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public decimal BundlePrice { get; set; }

    public int Quantity { get; set; }

    [ForeignKey("SKU")]
    public int? SKUId { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    public SKU? SKU { get; set; }
}
