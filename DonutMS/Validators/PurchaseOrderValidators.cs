using FluentValidation;
using DonutMS.Core.Domain;
using DonutMS.Data.Entities;

namespace DonutMS.Validators;

public class PurchaseOrderValidator : AbstractValidator<PurchaseOrder>
{
    public PurchaseOrderValidator()
    {
        RuleFor(x => x.PONumber)
            .NotEmpty().WithMessage("PO number is required")
            .Length(1, 50).WithMessage("PO number must be between 1 and 50 characters");

        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithMessage("Supplier is required");

        RuleFor(x => x.OrderDate)
            .NotEmpty().WithMessage("Order date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Order date cannot be in the future");

        RuleFor(x => x.RequiredDeliveryDate)
            .GreaterThan(x => x.OrderDate)
            .WithMessage("Required delivery date must be after order date");

        RuleFor(x => x.ActualDeliveryDate)
            .GreaterThanOrEqualTo(x => x.OrderDate)
            .When(x => x.ActualDeliveryDate.HasValue)
            .WithMessage("Actual delivery date cannot be before order date");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be greater than 0");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(x => DomainConstants.PurchaseOrderStatus.All.Contains(x))
            .WithMessage($"Status must be '{DomainConstants.PurchaseOrderStatus.Draft}', '{DomainConstants.PurchaseOrderStatus.Confirmed}', '{DomainConstants.PurchaseOrderStatus.Sent}', '{DomainConstants.PurchaseOrderStatus.Received}', or '{DomainConstants.PurchaseOrderStatus.Cancelled}'");
    }
}

public class PurchaseOrderItemValidator : AbstractValidator<PurchaseOrderItem>
{
    public PurchaseOrderItemValidator()
    {
        RuleFor(x => x.PurchaseOrderId)
            .GreaterThan(0).WithMessage("Purchase order is required");

        RuleFor(x => x.IngredientId)
            .GreaterThan(0).WithMessage("Ingredient is required");

        RuleFor(x => x.LineNumber)
            .GreaterThan(0).WithMessage("Line number must be greater than 0");

        RuleFor(x => x.OrderedQuantity)
            .GreaterThan(0).WithMessage("Ordered quantity must be greater than 0");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("Unit is required");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than 0");

        RuleFor(x => x.LineTotal)
            .GreaterThan(0).WithMessage("Line total must be greater than 0");

        RuleFor(x => x.ReceivedQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ReceivedQuantity.HasValue)
            .WithMessage("Received quantity cannot be negative")
            .LessThanOrEqualTo(x => x.OrderedQuantity)
            .When(x => x.ReceivedQuantity.HasValue)
            .WithMessage("Received quantity cannot exceed ordered quantity");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(x => DomainConstants.PurchaseOrderItemStatus.All.Contains(x))
            .WithMessage($"Status must be '{DomainConstants.PurchaseOrderItemStatus.Pending}', '{DomainConstants.PurchaseOrderItemStatus.Partial}', '{DomainConstants.PurchaseOrderItemStatus.Received}', or '{DomainConstants.PurchaseOrderItemStatus.Cancelled}'");
    }
}

public class PurchaseOrderReceivingValidator : AbstractValidator<PurchaseOrderReceiving>
{
    public PurchaseOrderReceivingValidator()
    {
        RuleFor(x => x.PurchaseOrderId)
            .GreaterThan(0).WithMessage("Purchase order is required");

        RuleFor(x => x.ReceivingDate)
            .NotEmpty().WithMessage("Receiving date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Receiving date cannot be in the future");

        RuleFor(x => x.TotalReceivedQuantity)
            .GreaterThan(0).WithMessage("Total received quantity must be greater than 0");
    }
}

public class PurchaseOrderReceivingDetailValidator : AbstractValidator<PurchaseOrderReceivingDetail>
{
    public PurchaseOrderReceivingDetailValidator()
    {
        RuleFor(x => x.ReceivingId)
            .GreaterThan(0).WithMessage("Receiving record is required");

        RuleFor(x => x.PurchaseOrderItemId)
            .GreaterThan(0).WithMessage("Purchase order item is required");

        RuleFor(x => x.ReceivedQuantity)
            .GreaterThan(0).WithMessage("Received quantity must be greater than 0");

        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.ExpiryDate.HasValue)
            .WithMessage("Expiry date must be in the future");
    }
}
