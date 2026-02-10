using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DonutMS.Core.MVVM;

namespace DonutMS.Data.Entities;

[Table("Operators")]
public class Operator : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [StringLength(50)]
    public required string EmployeeId { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public DateTime HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    [StringLength(50)]
    public string? JobTitle { get; set; }

    public decimal BaseSalary { get; set; }

    [StringLength(50)]
    public string SalaryPeriod { get; set; } = "Monthly";

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<LaborRate>? LaborRates { get; set; }
    public ICollection<BatchLabor>? BatchLaborRecords { get; set; }
}

[Table("LaborRates")]
public class LaborRate : BaseModel
{
    [ForeignKey("Operator")]
    public int OperatorId { get; set; }

    public decimal HourlyRate { get; set; }

    public decimal OvertimeMultiplier { get; set; } = 1.5m;

    public DateTime EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(50)]
    public string? ShiftType { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    public Operator? Operator { get; set; }
}

[Table("EquipmentDepreciation")]
public class EquipmentDepreciation : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string EquipmentName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public decimal AcquisitionCost { get; set; }

    public DateTime AcquisitionDate { get; set; }

    public int DepreciationYears { get; set; }

    [StringLength(50)]
    public string DepreciationMethod { get; set; } = "StraightLine";

    public decimal ResidualValue { get; set; }

    public decimal MonthlyDepreciation { get; set; }

    public DateTime? DisposalDate { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }
}

[Table("UtilityExpenses")]
public class UtilityExpense : BaseModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public required string UtilityType { get; set; }

    public decimal MonthlyAmount { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(50)]
    public string AllocationMethod { get; set; } = "Percentage";

    public decimal AllocationValue { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }
}
