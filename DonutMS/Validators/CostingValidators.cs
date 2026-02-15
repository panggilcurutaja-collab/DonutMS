using FluentValidation;
using DonutMS.Data.Entities;

namespace DonutMS.Validators;

public class SKUValidator : AbstractValidator<SKU>
{
    public SKUValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("SKU name is required")
            .Length(1, 100).WithMessage("SKU name must be between 1 and 100 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("SKU code is required")
            .Length(1, 50).WithMessage("SKU code must be between 1 and 50 characters")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("SKU code must contain only uppercase letters, numbers, and hyphens");

        RuleFor(x => x.RetailPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Retail price cannot be negative");
    }
}

public class SKUCostValidator : AbstractValidator<SKUCost>
{
    public SKUCostValidator()
    {
        RuleFor(x => x.SKUId)
            .GreaterThan(0).WithMessage("SKU is required");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Effective date cannot be in the future");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.EffectiveDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after effective date");

        RuleFor(x => x.MaterialCost)
            .GreaterThanOrEqualTo(0).WithMessage("Material cost cannot be negative");

        RuleFor(x => x.PackagingCost)
            .GreaterThanOrEqualTo(0).WithMessage("Packaging cost cannot be negative");

        RuleFor(x => x.LaborCost)
            .GreaterThanOrEqualTo(0).WithMessage("Labor cost cannot be negative");

        RuleFor(x => x.OverheadCost)
            .GreaterThanOrEqualTo(0).WithMessage("Overhead cost cannot be negative");

        RuleFor(x => x.TotalHPP)
            .GreaterThan(0).WithMessage("Total HPP must be greater than 0");

        RuleFor(x => x.GrossMargin)
            .GreaterThanOrEqualTo(-100).WithMessage("Gross margin cannot be less than -100%")
            .LessThanOrEqualTo(100).WithMessage("Gross margin cannot exceed 100%");
    }
}

public class PriceHistoryValidator : AbstractValidator<PriceHistory>
{
    public PriceHistoryValidator()
    {
        RuleFor(x => x.SKUId)
            .GreaterThan(0).WithMessage("SKU is required");

        RuleFor(x => x.OldPrice)
            .GreaterThan(0).WithMessage("Old price must be greater than 0");

        RuleFor(x => x.NewPrice)
            .GreaterThan(0).WithMessage("New price must be greater than 0");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required");
    }
}

public class OperatingCostValidator : AbstractValidator<OperatingCost>
{
    public OperatingCostValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Cost name is required")
            .Length(1, 100).WithMessage("Cost name must be between 1 and 100 characters");

        RuleFor(x => x.CostType)
            .NotEmpty().WithMessage("Cost type is required")
            .Must(x => x == "Fixed" || x == "Variable")
            .WithMessage("Cost type must be 'Fixed' or 'Variable'");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.AllocationMethod)
            .NotEmpty().WithMessage("Allocation method is required")
            .Must(x => x == "Percentage" || x == "Fixed" || x == "PerUnit")
            .WithMessage("Allocation method must be 'Percentage', 'Fixed', or 'PerUnit'");

        RuleFor(x => x.AllocationValue)
            .GreaterThanOrEqualTo(0).WithMessage("Allocation value cannot be negative");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.EffectiveDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after effective date");
    }
}

public class DiscountValidator : AbstractValidator<Discount>
{
    public DiscountValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Discount name is required")
            .Length(1, 100).WithMessage("Discount name must be between 1 and 100 characters");

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("Discount type is required")
            .Must(x => x == "Percentage" || x == "Fixed")
            .WithMessage("Discount type must be 'Percentage' or 'Fixed'");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date");

        RuleFor(x => x.MinimumQuantity)
            .GreaterThan(0)
            .When(x => x.MinimumQuantity.HasValue)
            .WithMessage("Minimum quantity must be greater than 0");
    }
}

public class BundlePackageValidator : AbstractValidator<BundlePackage>
{
    public BundlePackageValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Bundle name is required")
            .Length(1, 100).WithMessage("Bundle name must be between 1 and 100 characters");

        RuleFor(x => x.BundlePrice)
            .GreaterThan(0).WithMessage("Bundle price must be greater than 0");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Bundle quantity must be greater than 0");
    }
}
