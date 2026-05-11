using HotelPOS.Application.Interface;
using System.Windows;

namespace HotelPOS.Infrastructure
{
    public class ThemeService : IThemeService
    {
        public bool IsDarkMode { get; private set; }

        public void ToggleTheme()
        {
            ApplyTheme(!IsDarkMode);
        }

        public void ApplyTheme(bool isDark)
        {
            IsDarkMode = isDark;
            var app = System.Windows.Application.Current;
            var dicts = app.Resources.MergedDictionaries;

            var themeUri = isDark
                ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

            // Find and replace the theme dictionary
            var existingTheme = dicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml"));
            if (existingTheme != null) dicts.Remove(existingTheme);

            dicts.Insert(0, new ResourceDictionary { Source = themeUri });
        }
    }
}
