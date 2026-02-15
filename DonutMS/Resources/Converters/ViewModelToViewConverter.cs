using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DonutMS.Services;

namespace DonutMS.Resources.Converters;

/// <summary>
/// Converter untuk otomatis locate dan create View berdasarkan ViewModel
/// </summary>
public class ViewModelToViewConverter : IValueConverter
{
    private IViewLocator? _viewLocator;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value == null)
            return null;

        // Get ViewLocator dari service provider jika belum ada
        if (_viewLocator == null && Application.Current is App app && App.ServiceProvider != null)
        {
            _viewLocator = App.ServiceProvider.GetService(typeof(IViewLocator)) as IViewLocator;
        }

        if (_viewLocator == null)
        {
            System.Diagnostics.Debug.WriteLine("ViewLocator service not available");
            return null;
        }

        var viewModelType = value.GetType();
        var view = _viewLocator.GetViewForViewModel(viewModelType);
        
        if (view == null)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to locate view for {viewModelType.Name}");
            return CreatePlaceholder(viewModelType.Name);
        }

        // Set DataContext ke ViewModel
        if (view is FrameworkElement frameworkElement)
        {
            frameworkElement.DataContext = value;
        }

        return view;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        return null;
    }

    private UIElement CreatePlaceholder(string viewModelName)
    {
        var textBlock = new TextBlock
        {
            Text = $"No view found for {viewModelName}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = System.Windows.Media.Brushes.Red,
            FontSize = 14
        };
        return textBlock;
    }
}
