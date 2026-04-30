using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using System.Drawing.Printing;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly ISettingService _settingService;
        private readonly IUserService _userService;
        private readonly UsersView _usersView;
        private SystemSetting? _current;

        public SettingsView(ISettingService settingService, IUserService userService)
        {
            InitializeComponent();
            _settingService = settingService;
            _userService = userService;

            // Embed UsersView into the Users tab
            _usersView = new UsersView(_userService);
            UsersHost.Content = _usersView;

            Loaded += async (s, e) =>
            {
                LoadPrinters();
                await LoadSettingsAsync();
                await _usersView.RefreshAsync();
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
            _current = await _settingService.GetSettingsAsync();

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
            await Save();
        }

        private async Task Save()
        {
            try
            {
                await _settingService.SaveSettingsAsync(_current!);
                MessageBox.Show("Settings saved successfully.", "✅  Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
