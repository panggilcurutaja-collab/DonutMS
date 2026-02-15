using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DonutMS.Resources.Converters;

public class CurrencyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is decimal dec)
            return $"Rp {dec:N0}";
        if (value is double dbl)
            return $"Rp {dbl:N0}";
        if (value is int i)
            return $"Rp {i:N0}";
        return value?.ToString() ?? "Rp 0";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is string str)
        {
            str = str.Replace("Rp ", "").Replace(".", "").Trim();
            if (decimal.TryParse(str, out var result))
                return result;
        }
        return 0m;
    }
}

/// <summary>
/// Converts boolean to GridLength for collapsible sidebar menu.
/// Parameter format: "expandedWidth,collapsedWidth" (e.g., "250,50")
/// </summary>
public class BoolToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is not bool isExpanded)
            return new GridLength(50);

        if (parameter is string paramStr)
        {
            var parts = paramStr.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out var expandedWidth) &&
                double.TryParse(parts[1], out var collapsedWidth))
            {
                return new GridLength(isExpanded ? expandedWidth : collapsedWidth);
            }
        }

        return new GridLength(isExpanded ? 250 : 50);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        return Binding.DoNothing;
    }
}


public class PercentageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is decimal dec)
            return $"{dec:N1}%";
        if (value is double dbl)
            return $"{dbl:N1}%";
        if (value is int i)
            return $"{i}%";
        return value?.ToString() ?? "0%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is string str)
        {
            str = str.Replace("%", "").Trim();
            if (decimal.TryParse(str, out var result))
                return result;
        }
        return 0m;
    }
}

public class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        var status = value?.ToString()?.ToLower() ?? "";

        return status switch
        {
            "completed" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
            "in progress" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
            "planned" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
            "cancelled" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
            "pending" => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Yellow
            _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Grey
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        return Binding.DoNothing;
    }
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is bool b)
            return b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        return value is System.Windows.Visibility vis && vis == System.Windows.Visibility.Visible;
    }
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        var result = value is bool b ? !b : true;

        if (parameter is string param && param.Equals("Visibility", StringComparison.OrdinalIgnoreCase))
        {
            return result ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        return result;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is System.Windows.Visibility vis)
        {
            return vis != System.Windows.Visibility.Visible;
        }

        return value is bool b ? !b : true;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        return value == null ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        return Binding.DoNothing;
    }
}

public class StringEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        var str = value?.ToString() ?? "";
        return string.IsNullOrWhiteSpace(str) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        return Binding.DoNothing;
    }
}

public class QuantityHighlightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is decimal dec && dec <= 0)
            return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red for low/zero stock

        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green for good stock
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        return Binding.DoNothing;
    }
}

public class IconToGlyphConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Home", "\uE80F" },
        { "Palette", "\uE790" },
        { "SwapHorizontal", "\uE8AB" },
        { "Calculator", "\uE8EF" },
        { "Tag", "\uE8EC" },
        { "Package", "\uE7B8" },
        { "Truck", "\uE804" },
        { "Wrench", "\uE7AD" },
        { "CurrencyUsd", "\uEAFD" },
        { "AccountGroup", "\uE716" },
        { "Percent", "\uE94C" },
        { "ChartBar", "\uE9D2" },
        { "ShieldAccount", "\uE7EE" }
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        var key = value?.ToString() ?? string.Empty;
        return Map.TryGetValue(key, out var glyph) ? glyph : "\uE10C";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        return Binding.DoNothing;
    }
}

public class IconToPackIconMaterialKindConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        var key = value?.ToString() ?? string.Empty;
        return key switch
        {
            "Home" => "Home",
            "Palette" => "Palette",
            "SwapHorizontal" => "SwapHorizontal",
            "Calculator" => "Calculator",
            "Tag" => "Tag",
            "Package" => "PackageVariant",
            "Truck" => "Truck",
            "Wrench" => "Wrench",
            "CurrencyUsd" => "CurrencyUsd",
            "AccountGroup" => "AccountGroup",
            "Percent" => "Percent",
            "ChartBar" => "ChartBar",
            "ShieldAccount" => "ShieldAccount",
            _ => "ViewDashboard"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        return Binding.DoNothing;
    }
}
