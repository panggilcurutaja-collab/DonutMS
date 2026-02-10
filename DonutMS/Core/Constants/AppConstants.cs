namespace DonutMS.Core.Constants;

public static class AppConstants
{
    public const string AppName = "Donut Management System";
    public const string AppVersion = "1.0.0";
    public const string DatabaseFileName = "donutms.db";
    public const string LogsFolder = "Logs";

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string ProductionManager = "ProductionManager";
        public const string Operator = "Operator";
        public const string Cashier = "Cashier";
    }

    public static class UnitTypes
    {
        public const string Kilogram = "kg";
        public const string Gram = "g";
        public const string Liter = "L";
        public const string Milliliter = "ml";
        public const string Piece = "pcs";
        public const string Box = "box";
        public const string Batch = "batch";
    }

    public static class DateTimeFormats
    {
        public const string DisplayDate = "dd MMMM yyyy";
        public const string DisplayDateTime = "dd MMMM yyyy HH:mm:ss";
        public const string DisplayTime = "HH:mm:ss";
    }
}
