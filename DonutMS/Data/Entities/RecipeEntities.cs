using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DonutMS.Core.MVVM;

namespace DonutMS.Data.Entities;

[Table("Recipes")]
public class Recipe : BaseModel
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [Required]
    [StringLength(50)]
    public required string Code { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public int? CurrentVersionId { get; set; }

    public decimal YieldPerBatch { get; set; }

    [ForeignKey("YieldUnit")]
    public int YieldUnitId { get; set; }

    public decimal EstimatedProductionTime { get; set; }

    [StringLength(50)]
    public string? EstimatedProductionTimeUnit { get; set; } = "minutes";

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    public Unit YieldUnit { get; set; } = null!;
    public RecipeVersion? CurrentVersion { get; set; }
    public ICollection<RecipeVersion> Versions { get; set; } = new List<RecipeVersion>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<SKU> SKUs { get; set; } = new List<SKU>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}

[Table("RecipeVersions")]
public class RecipeVersion : BaseModel
{
    [ForeignKey("Recipe")]
    public int RecipeId { get; set; }

    public int VersionNumber { get; set; }

    [StringLength(500)]
    public string? ChangeNotes { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? ObsoleteDate { get; set; }

    public bool IsActive { get; set; } = true;

    public decimal YieldPerBatch { get; set; }

    [ForeignKey("YieldUnit")]
    public int YieldUnitId { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public Unit YieldUnit { get; set; } = null!;
    public ICollection<RecipeVersionIngredient> Ingredients { get; set; } = new List<RecipeVersionIngredient>();
}

[Table("RecipeIngredients")]
public class RecipeIngredient : BaseModel
{
    [ForeignKey("Recipe")]
    public int RecipeId { get; set; }

    [ForeignKey("Ingredient")]
    public int IngredientId { get; set; }

    public decimal QuantityPerBatch { get; set; }

    [ForeignKey("Unit")]
    public int UnitId { get; set; }

    public int SortOrder { get; set; }

    public bool IsOptional { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public Ingredient Ingredient { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}

[Table("RecipeVersionIngredients")]
public class RecipeVersionIngredient : BaseModel
{
    [ForeignKey("RecipeVersion")]
    public int RecipeVersionId { get; set; }

    [ForeignKey("Ingredient")]
    public int IngredientId { get; set; }

    public decimal QuantityPerBatch { get; set; }

    [ForeignKey("Unit")]
    public int UnitId { get; set; }

    public int SortOrder { get; set; }

    public bool IsOptional { get; set; }

    public decimal WastePercentage { get; set; } = 0;

    [StringLength(500)]
    public string? Notes { get; set; }

    public RecipeVersion RecipeVersion { get; set; } = null!;
    public Ingredient Ingredient { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}

[Table("RecipeSubstitutions")]
public class RecipeSubstitution : BaseModel
{
    [ForeignKey("OriginalIngredient")]
    public int OriginalIngredientId { get; set; }

    [ForeignKey("SubstituteIngredient")]
    public int SubstituteIngredientId { get; set; }

    public decimal SubstitutionRatio { get; set; }

    public decimal CostImpact { get; set; }

    public bool IsApproved { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public Ingredient OriginalIngredient { get; set; } = null!;
    public Ingredient SubstituteIngredient { get; set; } = null!;
}
