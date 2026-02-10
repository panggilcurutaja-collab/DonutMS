using FluentValidation;
using DonutMS.Data.Entities;

namespace DonutMS.Validators;

public class BatchValidator : AbstractValidator<Batch>
{
    public BatchValidator()
    {
        RuleFor(x => x.BatchCode)
            .NotEmpty().WithMessage("Batch code is required")
            .Length(1, 50).WithMessage("Batch code must be between 1 and 50 characters")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Batch code must contain only uppercase letters, numbers, and hyphens");

        RuleFor(x => x.RecipeId)
            .GreaterThan(0).WithMessage("Recipe is required");

        RuleFor(x => x.ProductionDate)
            .NotEmpty().WithMessage("Production date is required");

        RuleFor(x => x.TargetYield)
            .GreaterThan(0).WithMessage("Target yield must be greater than 0");

        RuleFor(x => x.YieldUnitId)
            .GreaterThan(0).WithMessage("Yield unit is required");

        RuleFor(x => x.ActualYield)
            .GreaterThan(0)
            .When(x => x.ActualYield.HasValue)
            .WithMessage("Actual yield must be greater than 0");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(x => x == "Planned" || x == "In Progress" || x == "Completed" || x == "Cancelled")
            .WithMessage("Status must be 'Planned', 'In Progress', 'Completed', or 'Cancelled'");
    }
}

public class BatchIngredientValidator : AbstractValidator<BatchIngredient>
{
    public BatchIngredientValidator()
    {
        RuleFor(x => x.BatchId)
            .GreaterThan(0).WithMessage("Batch is required");

        RuleFor(x => x.IngredientId)
            .GreaterThan(0).WithMessage("Ingredient is required");

        RuleFor(x => x.PlannedQuantity)
            .GreaterThan(0).WithMessage("Planned quantity must be greater than 0");

        RuleFor(x => x.PlannedUnitId)
            .GreaterThan(0).WithMessage("Planned unit is required");

        RuleFor(x => x.ActualQuantity)
            .GreaterThan(0)
            .When(x => x.ActualQuantity.HasValue)
            .WithMessage("Actual quantity must be greater than 0");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(x => x == "Planned" || x == "Allocated" || x == "Used" || x == "Wasted")
            .WithMessage("Status must be 'Planned', 'Allocated', 'Used', or 'Wasted'");
    }
}

public class QualityControlValidator : AbstractValidator<QualityControl>
{
    public QualityControlValidator()
    {
        RuleFor(x => x.BatchId)
            .GreaterThan(0).WithMessage("Batch is required");

        RuleFor(x => x.InspectionDate)
            .NotEmpty().WithMessage("Inspection date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Inspection date cannot be in the future");

        RuleFor(x => x.Taste)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Taste.HasValue)
            .WithMessage("Taste score cannot be negative")
            .LessThanOrEqualTo(10)
            .When(x => x.Taste.HasValue)
            .WithMessage("Taste score cannot exceed 10");

        RuleFor(x => x.Texture)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Texture.HasValue)
            .WithMessage("Texture score cannot be negative")
            .LessThanOrEqualTo(10)
            .When(x => x.Texture.HasValue)
            .WithMessage("Texture score cannot exceed 10");

        RuleFor(x => x.Appearance)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Appearance.HasValue)
            .WithMessage("Appearance score cannot be negative")
            .LessThanOrEqualTo(10)
            .When(x => x.Appearance.HasValue)
            .WithMessage("Appearance score cannot exceed 10");

        RuleFor(x => x.Aroma)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Aroma.HasValue)
            .WithMessage("Aroma score cannot be negative")
            .LessThanOrEqualTo(10)
            .When(x => x.Aroma.HasValue)
            .WithMessage("Aroma score cannot exceed 10");
    }
}

public class BatchLaborValidator : AbstractValidator<BatchLabor>
{
    public BatchLaborValidator()
    {
        RuleFor(x => x.BatchId)
            .GreaterThan(0).WithMessage("Batch is required");

        RuleFor(x => x.OperatorId)
            .GreaterThan(0).WithMessage("Operator is required");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time");

        RuleFor(x => x.HoursWorked)
            .GreaterThan(0).WithMessage("Hours worked must be greater than 0");

        RuleFor(x => x.OvertimeHours)
            .GreaterThanOrEqualTo(0).WithMessage("Overtime hours cannot be negative");

        RuleFor(x => x)
            .Custom((record, context) =>
            {
                var calculatedHours = (decimal)(record.EndTime - record.StartTime).TotalHours;
                if (Math.Abs(calculatedHours - record.HoursWorked) > 0.1m)
                {
                    context.AddFailure("Hours worked do not match the difference between start and end time");
                }
            });
    }
}
