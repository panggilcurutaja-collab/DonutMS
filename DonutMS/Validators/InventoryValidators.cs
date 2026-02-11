using FluentValidation;
using DonutMS.Core.Domain;
using DonutMS.Data.Entities;

namespace DonutMS.Validators;

public class IngredientValidator : AbstractValidator<Ingredient>
{
    public IngredientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ingredient name is required")
            .Length(1, 100).WithMessage("Ingredient name must be between 1 and 100 characters");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("Ingredient SKU is required")
            .Length(1, 50).WithMessage("Ingredient SKU must be between 1 and 50 characters")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("SKU must contain only uppercase letters, numbers, and hyphens");

        RuleFor(x => x.ConsumptionUnitId)
            .GreaterThan(0).WithMessage("Consumption unit is required");

        RuleFor(x => x.PurchaseUnitId)
            .GreaterThan(0).WithMessage("Purchase unit is required");

        RuleFor(x => x.MinimumStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level cannot be negative");

        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(x => x.MinimumStockLevel)
            .WithMessage("Reorder point must be greater than or equal to minimum stock level");

        RuleFor(x => x.ReorderQuantity)
            .GreaterThan(0).WithMessage("Reorder quantity must be greater than 0");

        RuleFor(x => x.ShelfLifeDays)
            .GreaterThan(0).WithMessage("Shelf life must be greater than 0 days");
    }
}

public class UnitValidator : AbstractValidator<Unit>
{
    public UnitValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Unit code is required")
            .Length(1, 50).WithMessage("Unit code must be between 1 and 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Unit name is required")
            .Length(1, 150).WithMessage("Unit name must be between 1 and 150 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Unit category is required")
            .Length(1, 50).WithMessage("Unit category must be between 1 and 50 characters");

        RuleFor(x => x.ConversionFactor)
            .GreaterThan(0).WithMessage("Conversion factor must be greater than 0");

        RuleFor(x => x.BaseUnit)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.BaseUnit))
            .WithMessage("Base unit must be 50 characters or less");
    }
}

public class SupplierValidator : AbstractValidator<Supplier>
{
    public SupplierValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Supplier name is required")
            .Length(1, 200).WithMessage("Supplier name must be between 1 and 200 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Supplier email is not valid")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Phone number must be 20 characters or less");

        RuleFor(x => x.MinimumOrderQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum order quantity cannot be negative");

        RuleFor(x => x.LeadTimeDays)
            .GreaterThanOrEqualTo(0).WithMessage("Lead time days cannot be negative");
    }
}

public class IngredientPriceValidator : AbstractValidator<IngredientPrice>
{
    public IngredientPriceValidator()
    {
        RuleFor(x => x.IngredientId)
            .GreaterThan(0).WithMessage("Ingredient is required");

        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithMessage("Supplier is required");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("Unit is required");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Effective date cannot be in the future");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.EffectiveDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after effective date");

        RuleFor(x => x.MinimumQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum quantity cannot be negative");
    }
}

public class InventoryStockValidator : AbstractValidator<InventoryStock>
{
    public InventoryStockValidator()
    {
        RuleFor(x => x.IngredientId)
            .GreaterThan(0).WithMessage("Ingredient is required");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("Unit is required");

        RuleFor(x => x.ReservedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Reserved quantity cannot be negative")
            .LessThanOrEqualTo(x => x.Quantity)
            .WithMessage("Reserved quantity cannot exceed total quantity");
    }
}

public class StockBatchValidator : AbstractValidator<StockBatch>
{
    public StockBatchValidator()
    {
        RuleFor(x => x.InventoryStockId)
            .GreaterThan(0).WithMessage("Inventory stock is required");

        RuleFor(x => x.ReceiptDate)
            .NotEmpty().WithMessage("Receipt date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Receipt date cannot be in the future");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.ReceiptDate)
            .When(x => x.ExpiryDate.HasValue)
            .WithMessage("Expiry date must be after receipt date");

        RuleFor(x => x.QuantityReceived)
            .GreaterThan(0).WithMessage("Quantity received must be greater than 0");

        RuleFor(x => x.QuantityUsed)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity used cannot be negative")
            .LessThanOrEqualTo(x => x.QuantityReceived)
            .WithMessage("Quantity used cannot exceed quantity received");

        RuleFor(x => x.QuantityWasted)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity wasted cannot be negative")
            .LessThanOrEqualTo(x => x.QuantityReceived)
            .WithMessage("Quantity wasted cannot exceed quantity received");

        RuleFor(x => x)
            .Custom((batch, context) =>
            {
                if (batch.QuantityUsed + batch.QuantityWasted > batch.QuantityReceived)
                {
                    context.AddFailure("Total consumed quantity cannot exceed quantity received");
                }
            });
    }
}

public class StockTransactionValidator : AbstractValidator<StockTransaction>
{
    public StockTransactionValidator()
    {
        RuleFor(x => x.InventoryStockId)
            .GreaterThan(0).WithMessage("Inventory stock is required");

        RuleFor(x => x.TransactionType)
            .NotEmpty().WithMessage("Transaction type is required")
            .Must(x => DomainConstants.StockTransactionType.All.Contains(x))
            .WithMessage($"Transaction type must be '{DomainConstants.StockTransactionType.In}', '{DomainConstants.StockTransactionType.Out}', '{DomainConstants.StockTransactionType.Adjustment}', or '{DomainConstants.StockTransactionType.Waste}'");

        RuleFor(x => x.Quantity)
            .NotEmpty().WithMessage("Quantity is required")
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("Unit is required");

        RuleFor(x => x.TransactionDate)
            .NotEmpty().WithMessage("Transaction date is required");
    }
}
