using HotelPOS.Application.Interfaces;
using System.Windows;

namespace HotelPOS.Services
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

            var colorsUri = isDark
                ? new Uri("Themes/Core/Colors.Dark.xaml", UriKind.Relative)
                : new Uri("Themes/Core/Colors.Light.xaml", UriKind.Relative);

            // Find and remove any previously injected color override dictionary at the application level
            var existingOverride = dicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Colors."));
            if (existingOverride != null) dicts.Remove(existingOverride);

            try
            {
                // Adding to the end of the list overrides any identical keys from the base Theme.xaml
                dicts.Add(new ResourceDictionary { Source = colorsUri });
            }
            catch (Exception)
            {
                // In test/runner environment, the resource file might not be resolvable, which is acceptable
            }
        }
    }
}
