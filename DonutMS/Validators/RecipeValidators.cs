using FluentValidation;
using DonutMS.Data.Entities;

namespace DonutMS.Validators;

public class RecipeValidator : AbstractValidator<Recipe>
{
    public RecipeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Recipe name is required")
            .Length(1, 200).WithMessage("Recipe name must be between 1 and 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Recipe code is required")
            .Length(1, 50).WithMessage("Recipe code must be between 1 and 50 characters")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Recipe code must contain only uppercase letters, numbers, and hyphens");

        RuleFor(x => x.YieldPerBatch)
            .GreaterThan(0).WithMessage("Yield per batch must be greater than 0");

        RuleFor(x => x.YieldUnitId)
            .GreaterThan(0).WithMessage("Yield unit is required");

        RuleFor(x => x.EstimatedProductionTime)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated production time cannot be negative");
    }
}

public class RecipeVersionValidator : AbstractValidator<RecipeVersion>
{
    public RecipeVersionValidator()
    {
        RuleFor(x => x.RecipeId)
            .GreaterThan(0).WithMessage("Recipe ID is required");

        RuleFor(x => x.VersionNumber)
            .GreaterThan(0).WithMessage("Version number must be greater than 0");

        RuleFor(x => x.YieldPerBatch)
            .GreaterThan(0).WithMessage("Yield per batch must be greater than 0");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required");

        RuleFor(x => x.ObsoleteDate)
            .GreaterThan(x => x.EffectiveDate)
            .When(x => x.ObsoleteDate.HasValue)
            .WithMessage("Obsolete date must be after effective date");
    }
}

public class RecipeIngredientValidator : AbstractValidator<RecipeIngredient>
{
    public RecipeIngredientValidator()
    {
        RuleFor(x => x.RecipeId)
            .GreaterThan(0).WithMessage("Recipe ID is required");

        RuleFor(x => x.IngredientId)
            .GreaterThan(0).WithMessage("Ingredient ID is required");

        RuleFor(x => x.QuantityPerBatch)
            .GreaterThan(0).WithMessage("Quantity per batch must be greater than 0");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("Unit is required");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order cannot be negative");
    }
}

public class RecipeVersionIngredientValidator : AbstractValidator<RecipeVersionIngredient>
{
    public RecipeVersionIngredientValidator()
    {
        RuleFor(x => x.RecipeVersionId)
            .GreaterThan(0).WithMessage("Recipe version ID is required");

        RuleFor(x => x.IngredientId)
            .GreaterThan(0).WithMessage("Ingredient ID is required");

        RuleFor(x => x.QuantityPerBatch)
            .GreaterThan(0).WithMessage("Quantity per batch must be greater than 0");

        RuleFor(x => x.WastePercentage)
            .GreaterThanOrEqualTo(0).WithMessage("Waste percentage cannot be negative")
            .LessThanOrEqualTo(100).WithMessage("Waste percentage cannot exceed 100%");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("Unit is required");
    }
}

public class RecipeSubstitutionValidator : AbstractValidator<RecipeSubstitution>
{
    public RecipeSubstitutionValidator()
    {
        RuleFor(x => x.OriginalIngredientId)
            .GreaterThan(0).WithMessage("Original ingredient is required");

        RuleFor(x => x.SubstituteIngredientId)
            .GreaterThan(0).WithMessage("Substitute ingredient is required")
            .NotEqual(x => x.OriginalIngredientId)
            .WithMessage("Substitute ingredient must be different from original ingredient");

        RuleFor(x => x.SubstitutionRatio)
            .GreaterThan(0).WithMessage("Substitution ratio must be greater than 0");

        RuleFor(x => x.CostImpact)
            .GreaterThanOrEqualTo(-100).WithMessage("Cost impact cannot be less than -100%")
            .LessThanOrEqualTo(1000).WithMessage("Cost impact exceeds reasonable bounds");
    }
}
