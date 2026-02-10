using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DonutMS.Core.MVVM;

namespace DonutMS.Data.Entities;

[Table("Batches")]
public class Batch : BaseModel
{
    [Required]
    [StringLength(50)]
    public required string BatchCode { get; set; }

    [ForeignKey("Recipe")]
    public int RecipeId { get; set; }

    [ForeignKey("RecipeVersion")]
    public int? RecipeVersionId { get; set; }

    public DateTime ProductionDate { get; set; }

    public decimal TargetYield { get; set; }

    [ForeignKey("YieldUnit")]
    public int YieldUnitId { get; set; }

    public decimal? ActualYield { get; set; }

    public decimal? WasteQuantity { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Planned";

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool HasQCPass { get; set; }

    public Recipe? Recipe { get; set; }
    public RecipeVersion? RecipeVersion { get; set; }
    public Unit? YieldUnit { get; set; }
    public ICollection<BatchIngredient>? Ingredients { get; set; }
    public ICollection<BatchLabor>? LaborRecords { get; set; }
    public ICollection<QualityControl>? QualityControls { get; set; }
    public ICollection<StockTransaction>? StockTransactions { get; set; }
}

[Table("BatchIngredients")]
public class BatchIngredient : BaseModel
{
    [ForeignKey("Batch")]
    public int BatchId { get; set; }

    [ForeignKey("Ingredient")]
    public int IngredientId { get; set; }

    public decimal PlannedQuantity { get; set; }

    [ForeignKey("PlannedUnit")]
    public int PlannedUnitId { get; set; }

    public decimal? ActualQuantity { get; set; }

    [ForeignKey("StockBatch")]
    public int? StockBatchId { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Planned";

    [StringLength(500)]
    public string? Notes { get; set; }

    public Batch? Batch { get; set; }
    public Ingredient? Ingredient { get; set; }
    public Unit? PlannedUnit { get; set; }
    public StockBatch? StockBatch { get; set; }
}

[Table("QualityControls")]
public class QualityControl : BaseModel
{
    [ForeignKey("Batch")]
    public int BatchId { get; set; }

    public DateTime InspectionDate { get; set; }

    [StringLength(100)]
    public string? InspectedBy { get; set; }

    public bool Passed { get; set; }

    public decimal? Taste { get; set; }

    public decimal? Texture { get; set; }

    public decimal? Appearance { get; set; }

    public decimal? Aroma { get; set; }

    [StringLength(500)]
    public string? DefectsFound { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public Batch? Batch { get; set; }
}

[Table("BatchLaborRecords")]
public class BatchLabor : BaseModel
{
    [ForeignKey("Batch")]
    public int BatchId { get; set; }

    [ForeignKey("Operator")]
    public int OperatorId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public decimal HoursWorked { get; set; }

    [StringLength(50)]
    public string? ShiftType { get; set; }

    public decimal OvertimeHours { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public Batch? Batch { get; set; }
    public Operator? Operator { get; set; }
}
