namespace DonutMS.Core.Utils;

public static class UnitConverter
{
    private static readonly Dictionary<string, decimal> GramConversion = new()
    {
        { "kg", 1000 },
        { "g", 1 },
        { "mg", 0.001m }
    };

    private static readonly Dictionary<string, decimal> LiterConversion = new()
    {
        { "L", 1 },
        { "ml", 0.001m },
        { "cl", 0.01m }
    };

    public static decimal ConvertWeight(decimal value, string fromUnit, string toUnit)
    {
        if (!GramConversion.ContainsKey(fromUnit.ToLower()))
            throw new ArgumentException($"Unknown weight unit: {fromUnit}");
        if (!GramConversion.ContainsKey(toUnit.ToLower()))
            throw new ArgumentException($"Unknown weight unit: {toUnit}");

        var valueInGrams = value * GramConversion[fromUnit.ToLower()];
        return valueInGrams / GramConversion[toUnit.ToLower()];
    }

    public static decimal ConvertVolume(decimal value, string fromUnit, string toUnit)
    {
        if (!LiterConversion.ContainsKey(fromUnit.ToLower()))
            throw new ArgumentException($"Unknown volume unit: {fromUnit}");
        if (!LiterConversion.ContainsKey(toUnit.ToLower()))
            throw new ArgumentException($"Unknown volume unit: {toUnit}");

        var valueInLiters = value * LiterConversion[fromUnit.ToLower()];
        return valueInLiters / LiterConversion[toUnit.ToLower()];
    }

    public static bool IsWeightUnit(string unit) => GramConversion.ContainsKey(unit.ToLower());
    public static bool IsVolumeUnit(string unit) => LiterConversion.ContainsKey(unit.ToLower());
}
