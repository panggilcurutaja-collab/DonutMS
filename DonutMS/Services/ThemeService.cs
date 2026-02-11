using System.Windows;
using ControlzEx.Theming;

namespace DonutMS.Services;

public interface IThemeService
{
    bool IsDarkTheme { get; }
    void ApplyTheme(bool isDark);
}

public class ThemeService : IThemeService
{
    private static readonly Uri MahAppsLight = new("pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml", UriKind.Absolute);
    private static readonly Uri MahAppsDark = new("pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Blue.xaml", UriKind.Absolute);
    private static readonly Uri MaterialLight = new("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml", UriKind.Absolute);
    private static readonly Uri MaterialDark = new("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml", UriKind.Absolute);
    private static readonly Uri CustomLight = new("pack://application:,,,/DonutMS;component/Resources/Styles/Theme.Light.xaml", UriKind.Absolute);
    private static readonly Uri CustomDark = new("pack://application:,,,/DonutMS;component/Resources/Styles/Theme.Dark.xaml", UriKind.Absolute);

    public bool IsDarkTheme { get; private set; }

    public void ApplyTheme(bool isDark)
    {
        var app = Application.Current;
        if (app == null)
            return;

        IsDarkTheme = isDark;

        // Change MahApps theme (updates window chrome and controls)
        var mahAppsTheme = isDark ? "Dark.Blue" : "Light.Blue";
        ThemeManager.Current.ChangeTheme(app, mahAppsTheme);

        // Swap MaterialDesign dictionaries and custom overrides (search deep in merged dictionaries)
        var dictionaries = app.Resources.MergedDictionaries;
        ReplaceDictionaryDeep(dictionaries, MaterialLight, isDark ? MaterialDark : MaterialLight);
        ReplaceDictionaryDeep(dictionaries, MaterialDark, isDark ? MaterialDark : MaterialLight);
        ReplaceDictionaryDeep(dictionaries, CustomLight, isDark ? CustomDark : CustomLight);
        ReplaceDictionaryDeep(dictionaries, CustomDark, isDark ? CustomDark : CustomLight);
        ReplaceDictionaryDeep(dictionaries, MahAppsLight, isDark ? MahAppsDark : MahAppsLight);
        ReplaceDictionaryDeep(dictionaries, MahAppsDark, isDark ? MahAppsDark : MahAppsLight);
    }

    private static void ReplaceDictionaryDeep(System.Collections.ObjectModel.Collection<ResourceDictionary> dictionaries, Uri from, Uri to)
    {
        if (ReplaceInCollection(dictionaries, from, to))
            return;

        if (!ContainsDictionaryDeep(dictionaries, to))
            dictionaries.Add(new ResourceDictionary { Source = to });
    }

    private static bool ReplaceInCollection(System.Collections.ObjectModel.Collection<ResourceDictionary> dictionaries, Uri from, Uri to)
    {
        for (var i = 0; i < dictionaries.Count; i++)
        {
            var dict = dictionaries[i];
            if (dict.Source != null && dict.Source == from)
            {
                dictionaries[i] = new ResourceDictionary { Source = to };
                return true;
            }

            if (dict.MergedDictionaries.Count > 0 && ReplaceInCollection(dict.MergedDictionaries, from, to))
                return true;
        }

        return false;
    }

    private static bool ContainsDictionaryDeep(System.Collections.ObjectModel.Collection<ResourceDictionary> dictionaries, Uri source)
    {
        foreach (var dict in dictionaries)
        {
            if (dict.Source != null && dict.Source == source)
                return true;

            if (dict.MergedDictionaries.Count > 0 && ContainsDictionaryDeep(dict.MergedDictionaries, source))
                return true;
        }

        return false;
    }
}
