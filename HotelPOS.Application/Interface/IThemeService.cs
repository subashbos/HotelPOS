namespace HotelPOS.Application.Interface
{
    public interface IThemeService
    {
        bool IsDarkMode { get; }
        void ToggleTheme();
        void ApplyTheme(bool isDark);
    }
}
