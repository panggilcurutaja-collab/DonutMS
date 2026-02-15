namespace DonutMS.Core.Domain;

public static class DomainConstants
{
    public static class StockTransactionType
    {
        public const string In = "In";
        public const string Out = "Out";
        public const string Adjustment = "Adjustment";
        public const string Waste = "Waste";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            In, Out, Adjustment, Waste
        };
    }

    public static class StockBatchStatus
    {
        public const string Active = "Active";
        public const string Depleted = "Depleted";
        public const string Expired = "Expired";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            Active, Depleted, Expired
        };
    }

    public static class BatchStatus
    {
        public const string Planned = "Planned";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            Planned, InProgress, Completed, Cancelled
        };
    }

    public static class BatchIngredientStatus
    {
        public const string Planned = "Planned";
        public const string Allocated = "Allocated";
        public const string Consumed = "Consumed";
        public const string Used = "Used";
        public const string Wasted = "Wasted";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            Planned, Allocated, Consumed, Used, Wasted
        };
    }

    public static class PurchaseOrderStatus
    {
        public const string Draft = "Draft";
        public const string Confirmed = "Confirmed";
        public const string Sent = "Sent";
        public const string Received = "Received";
        public const string Cancelled = "Cancelled";
        public const string PartiallyReceived = "Partially Received";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            Draft, Confirmed, Sent, Received, Cancelled, PartiallyReceived
        };
    }

    public static class PurchaseOrderItemStatus
    {
        public const string Pending = "Pending";
        public const string Partial = "Partial";
        public const string Received = "Received";
        public const string Cancelled = "Cancelled";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            Pending, Partial, Received, Cancelled
        };
    }

    public static class DiscountType
    {
        public const string Percentage = "Percentage";
        public const string Fixed = "Fixed";
        public const string Volume = "Volume";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            Percentage, Fixed, Volume
        };
    }

    public static class CostType
    {
        public const string Fixed = "Fixed";
        public const string Variable = "Variable";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            Fixed, Variable
        };
    }

    public static class AllocationMethod
    {
        public const string Percentage = "Percentage";
        public const string PerUnit = "PerUnit";

        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            Percentage, PerUnit
        };
    }
}
