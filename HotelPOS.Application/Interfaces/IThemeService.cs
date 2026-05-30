namespace HotelPOS.Application.Interfaces
{
    public interface IThemeService
    {
        bool IsDarkMode { get; }
        void ToggleTheme();
        void ApplyTheme(bool isDark);
    }
}
