using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DonutMS.Core.MVVM;

namespace DonutMS.Data.Entities;

[Table("AuditLogs")]
public class AuditLog : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string EntityName { get; set; }

    public int EntityId { get; set; }

    [StringLength(50)]
    public required string Action { get; set; }

    [StringLength(100)]
    public string? UserId { get; set; }

    [StringLength(100)]
    public string? UserName { get; set; }

    public DateTime AuditDate { get; set; }

    [StringLength(500)]
    public string? OldValues { get; set; }

    [StringLength(500)]
    public string? NewValues { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    [StringLength(50)]
    public string? IPAddress { get; set; }
}

[Table("Allergens")]
public class Allergen : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Ingredient>? Ingredients { get; set; }
    public ICollection<SKUAllergen>? SKUAllergens { get; set; }
}

[Table("SKUAllergens")]
public class SKUAllergen : BaseModel
{
    [ForeignKey("SKU")]
    public int SKUId { get; set; }

    [ForeignKey("Allergen")]
    public int AllergenId { get; set; }

    public bool MayContainTrace { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public SKU? SKU { get; set; }
    public Allergen? Allergen { get; set; }
}

[Table("IngredientAllergens")]
public class IngredientAllergen : BaseModel
{
    [ForeignKey("Ingredient")]
    public int IngredientId { get; set; }

    [ForeignKey("Allergen")]
    public int AllergenId { get; set; }

    public bool IsContaminant { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public Ingredient? Ingredient { get; set; }
    public Allergen? Allergen { get; set; }
}

[Table("NutritionalInfos")]
public class NutritionalInfo : BaseModel
{
    [ForeignKey("SKU")]
    public int SKUId { get; set; }

    public decimal Calories { get; set; }

    public decimal Protein { get; set; }

    public decimal Fat { get; set; }

    public decimal Carbohydrates { get; set; }

    public decimal Fiber { get; set; }

    public decimal Sugar { get; set; }

    public decimal Sodium { get; set; }

    [StringLength(50)]
    public string? ServingSize { get; set; }

    public int? ServingsPerPackage { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public SKU? SKU { get; set; }
}

[Table("Users")]
public class User : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string Username { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? FullName { get; set; }

    public byte[]? PasswordHash { get; set; }

    public byte[]? PasswordSalt { get; set; }

    [StringLength(50)]
    public string Role { get; set; } = "Operator";

    public bool IsActive { get; set; } = true;

    public DateTime? LastLogin { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
