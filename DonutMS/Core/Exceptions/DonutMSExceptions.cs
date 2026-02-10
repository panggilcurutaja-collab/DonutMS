namespace DonutMS.Core.Exceptions;

public class DonutMSException : Exception
{
    public string? ErrorCode { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public DonutMSException(string message, string? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DonutMSException(string message, Exception innerException, string? errorCode = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class ValidationException : DonutMSException
{
    public ValidationException(string message) : base(message, "VALIDATION_ERROR") { }
}

public class BusinessLogicException : DonutMSException
{
    public BusinessLogicException(string message) : base(message, "BUSINESS_LOGIC_ERROR") { }
}

public class InsufficientStockException : DonutMSException
{
    public int IngredientId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }

    public InsufficientStockException(int ingredientId, decimal requested, decimal available)
        : base($"Insufficient stock. Ingredient: {ingredientId}, Requested: {requested}, Available: {available}", "INSUFFICIENT_STOCK")
    {
        IngredientId = ingredientId;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}

public class InvalidUnitConversionException : DonutMSException
{
    public InvalidUnitConversionException(string fromUnit, string toUnit)
        : base($"Cannot convert between {fromUnit} and {toUnit}. Units are not compatible.", "INVALID_UNIT_CONVERSION")
    {
    }
}

public class EntityNotFoundException : DonutMSException
{
    public string EntityType { get; set; }
    public int EntityId { get; set; }

    public EntityNotFoundException(string entityType, int entityId)
        : base($"{entityType} with ID {entityId} not found", "ENTITY_NOT_FOUND")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

public class DuplicateEntityException : DonutMSException
{
    public DuplicateEntityException(string entityType, string key)
        : base($"A {entityType} with {key} already exists", "DUPLICATE_ENTITY")
    {
    }
}

public class RecipeAlreadyInUseException : DonutMSException
{
    public RecipeAlreadyInUseException(int recipeId)
        : base($"Recipe {recipeId} is already being used in active batches", "RECIPE_IN_USE")
    {
    }
}

public class InvalidBatchStatusException : DonutMSException
{
    public InvalidBatchStatusException(string currentStatus, string attemptedAction)
        : base($"Cannot {attemptedAction} on batch in {currentStatus} status", "INVALID_BATCH_STATUS")
    {
    }
}
