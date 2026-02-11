using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DonutMS.Core.MVVM;

namespace DonutMS.Data.Entities;

[Table("Units")]
public class Unit : BaseModel
{
    [Required]
    [StringLength(50)]
    public required string Code { get; set; }

    [Required]
    [StringLength(150)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public required string Category { get; set; }

    public decimal ConversionFactor { get; set; } = 1;

    [StringLength(50)]
    public string? BaseUnit { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
}

[Table("Suppliers")]
public class Supplier : BaseModel
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? ContactPerson { get; set; }

    public decimal MinimumOrderQuantity { get; set; }

    public int LeadTimeDays { get; set; }

    [StringLength(50)]
    public string? PaymentTerms { get; set; }

    public bool IsActive { get; set; } = true;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public ICollection<IngredientPrice> IngredientPrices { get; set; } = new List<IngredientPrice>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

[Table("Ingredients")]
public class Ingredient : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [StringLength(50)]
    public required string SKU { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [ForeignKey("ConsumptionUnit")]
    public int ConsumptionUnitId { get; set; }

    [ForeignKey("PurchaseUnit")]
    public int PurchaseUnitId { get; set; }

    public decimal MinimumStockLevel { get; set; }

    public decimal ReorderPoint { get; set; }

    public decimal ReorderQuantity { get; set; }

    public int ShelfLifeDays { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    public Unit ConsumptionUnit { get; set; } = null!;
    public Unit PurchaseUnit { get; set; } = null!;
    public ICollection<IngredientPrice> Prices { get; set; } = new List<IngredientPrice>();
    public ICollection<InventoryStock> InventoryStocks { get; set; } = new List<InventoryStock>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<BatchIngredient> BatchIngredients { get; set; } = new List<BatchIngredient>();
    public ICollection<IngredientAllergen> Allergens { get; set; } = new List<IngredientAllergen>();
}

[Table("IngredientPrices")]
public class IngredientPrice : BaseModel
{
    [ForeignKey("Ingredient")]
    public int IngredientId { get; set; }

    [ForeignKey("Supplier")]
    public int SupplierId { get; set; }

    public decimal Price { get; set; }

    [ForeignKey("Unit")]
    public int UnitId { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public decimal MinimumQuantity { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public Ingredient Ingredient { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}
