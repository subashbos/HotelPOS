using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing.Printing;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly INotificationService _notificationService;
        private readonly UsersView _usersView;
        private SystemSetting? _current;

        public SettingsView(IUserService userService, IRoleService roleService, INotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;

            // Embed UsersView into the Users tab
            _usersView = new UsersView(userService, roleService);
            UsersHost.Content = _usersView;

            Loaded += async (s, e) =>
            {
                LoadPrinters();
                await LoadSettingsAsync();
                await _usersView.InitializeAsync();
            };
        }

        // ── Printers ─────────────────────────────────────────────────────────

        private void LoadPrinters()
        {
            try
            {
                PrinterList.Items.Clear();
                foreach (string p in PrinterSettings.InstalledPrinters)
                    PrinterList.Items.Add(p);
            }
            catch { /* silently skip if no printers installed */ }
        }

        // ── Load ─────────────────────────────────────────────────────────────

        private async Task LoadSettingsAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                _current = await settingService.GetSettingsAsync();
            }

            // Profile
            HotelNameBox.Text = _current.HotelName;
            HotelAddressBox.Text = _current.HotelAddress;
            HotelGstBox.Text = _current.HotelGst;
            HotelPhoneBox.Text = _current.HotelPhone;

            // Printer
            if (!string.IsNullOrEmpty(_current.DefaultPrinter) && PrinterList.Items.Contains(_current.DefaultPrinter))
                PrinterList.SelectedItem = _current.DefaultPrinter;
            FormatThermal.IsChecked = _current.ReceiptFormat == "Thermal";
            FormatA4.IsChecked = _current.ReceiptFormat == "A4";
            ShowPreviewCheck.IsChecked = _current.ShowPrintPreview;

            // Toggles
            ShowItemsCheck.IsChecked = _current.ShowItemsOnBill;
            ShowGstCheck.IsChecked = _current.ShowGstBreakdown;
            ShowDiscountCheck.IsChecked = _current.ShowDiscountLine;
            ShowPhoneCheck.IsChecked = _current.ShowPhoneOnReceipt;
            ShowFooterCheck.IsChecked = _current.ShowThankYouFooter;
            RoundOffCheck.IsChecked = _current.EnableRoundOff;
            CompositionCheck.IsChecked = _current.IsCompositionScheme;
        }

        // ── Save Hotel Profile ────────────────────────────────────────────────

        private async void SaveHotel_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;
            _current.HotelName = HotelNameBox.Text.Trim();
            _current.HotelAddress = HotelAddressBox.Text.Trim();
            _current.HotelGst = HotelGstBox.Text.Trim();
            _current.HotelPhone = HotelPhoneBox.Text.Trim();
            await Save();
        }

        // ── Save Printer & Display ────────────────────────────────────────────

        private async void SavePrinter_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;
            _current.DefaultPrinter = PrinterList.SelectedItem?.ToString() ?? "Microsoft Print to PDF";
            _current.ReceiptFormat = FormatThermal.IsChecked == true ? "Thermal" : "A4";
            _current.ShowPrintPreview = ShowPreviewCheck.IsChecked == true;

            _current.ShowItemsOnBill = ShowItemsCheck.IsChecked == true;
            _current.ShowGstBreakdown = ShowGstCheck.IsChecked == true;
            _current.ShowDiscountLine = ShowDiscountCheck.IsChecked == true;
            _current.ShowPhoneOnReceipt = ShowPhoneCheck.IsChecked == true;
            _current.ShowThankYouFooter = ShowFooterCheck.IsChecked == true;
            _current.EnableRoundOff = RoundOffCheck.IsChecked == true;
            _current.IsCompositionScheme = CompositionCheck.IsChecked == true;
            await Save();
        }

        private async Task Save()
        {
            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                    await settingService.SaveSettingsAsync(_current!);
                }
                _notificationService.ShowSuccess("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error saving settings: {ex.Message}");
            }
        }
    }
}
