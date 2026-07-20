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

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(userService);
                App.RegisterTestService(roleService);
                App.RegisterTestService(notificationService);
            }

            // Embed UsersView into the Users tab
            _usersView = new UsersView(userService, roleService);
            UsersHost.Content = _usersView;

            Loaded += async (s, e) =>
            {
                LoadPrinters();
                await LoadSettingsAsync();
            };
        }

        // ── Printers ─────────────────────────────────────────────────────────

        private void LoadPrinters() // NOSONAR
        {
            try
            {
                PrinterList.Items.Clear();
                foreach (string p in PrinterSettings.InstalledPrinters)
                    PrinterList.Items.Add(p);
            }
            catch { /* silently skip if no printers installed */ }
        }

        /// <summary>
        /// Load the current system settings and apply them to the view's UI controls.
        /// </summary>
        /// <returns>A task that completes when settings have been loaded and UI controls updated.</returns>

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

            // Disaster Recovery
            EnableAutomatedBackupsCheck.IsChecked = _current.EnableAutomatedBackups;
            OffsiteBackupPathBox.Text = _current.OffsiteBackupPath;

            // Security
            IdleTimeoutBox.Text = _current.IdleTimeoutMinutes.ToString();
            SmtpHostBox.Text = _current.SmtpHost;
            SmtpPortBox.Text = _current.SmtpPort.ToString();
            SmtpUseSslCheck.IsChecked = _current.SmtpUseSsl;
            SmtpUsernameBox.Text = _current.SmtpUsername;
            SmtpPasswordBox.Password = _current.SmtpPassword ?? string.Empty;
            SmtpFromBox.Text = _current.SmtpFromAddress;

            RefreshTwoFactorUi();
        }

        private void RefreshTwoFactorUi() // NOSONAR - updates this view instance's own XAML-named controls; cannot be static
        {
            var user = AppSession.CurrentUser;
            bool enabled = user?.TwoFactorEnabled == true;

            TwoFactorStatusText.Text = enabled ? "✅ Enabled on this account." : "⚪ Not enabled on this account.";
            EnableTwoFactorButton.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
            DisableTwoFactorButton.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void EnableTwoFactor_Click(object sender, RoutedEventArgs e)
        {
            var user = AppSession.CurrentUser;
            if (user == null) return;

            var dialog = new TwoFactorEnrollDialog(user.Username) { Owner = Window.GetWindow(this) };
            if (!dialog.ShowDialog().GetValueOrDefault()) return;

            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    await userService.SetTwoFactorAsync(user.Id, true, dialog.Secret);
                }
                user.TwoFactorEnabled = true;
                user.TwoFactorSecret = dialog.Secret;
                RefreshTwoFactorUi();
                _notificationService.ShowSuccess("Two-factor authentication is now enabled.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Could not enable two-factor authentication: {ex.Message}");
            }
        }

        private async void DisableTwoFactor_Click(object sender, RoutedEventArgs e)
        {
            var user = AppSession.CurrentUser;
            if (user == null) return;

            var confirm = await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessageAsync(
                "Disable two-factor authentication for your account?",
                "Confirm", HotelPOS.Application.Interfaces.DialogButton.YesNo, HotelPOS.Application.Interfaces.DialogIcon.Warning);
            if (confirm != HotelPOS.Application.Interfaces.DialogResult.Yes) return;

            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    await userService.SetTwoFactorAsync(user.Id, false, null);
                }
                user.TwoFactorEnabled = false;
                user.TwoFactorSecret = null;
                RefreshTwoFactorUi();
                _notificationService.ShowSuccess("Two-factor authentication has been disabled.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Could not disable two-factor authentication: {ex.Message}");
            }
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
            _current.ReceiptFormat = FormatThermal.IsChecked.GetValueOrDefault() ? "Thermal" : "A4";
            _current.ShowPrintPreview = ShowPreviewCheck.IsChecked.GetValueOrDefault();

            _current.ShowItemsOnBill = ShowItemsCheck.IsChecked.GetValueOrDefault();
            _current.ShowGstBreakdown = ShowGstCheck.IsChecked.GetValueOrDefault();
            _current.ShowDiscountLine = ShowDiscountCheck.IsChecked.GetValueOrDefault();
            _current.ShowPhoneOnReceipt = ShowPhoneCheck.IsChecked.GetValueOrDefault();
            _current.ShowThankYouFooter = ShowFooterCheck.IsChecked.GetValueOrDefault();
            _current.EnableRoundOff = RoundOffCheck.IsChecked.GetValueOrDefault();
            _current.IsCompositionScheme = CompositionCheck.IsChecked.GetValueOrDefault();
            await Save();
        }

        // ── Save Backup Configuration ──────────────────────────────────────────

        private async void SaveBackups_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;
            _current.EnableAutomatedBackups = EnableAutomatedBackupsCheck.IsChecked.GetValueOrDefault();
            _current.OffsiteBackupPath = string.IsNullOrWhiteSpace(OffsiteBackupPathBox.Text) ? null : OffsiteBackupPathBox.Text.Trim();
            await Save();
        }

        private async void BackupNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
                    await backup.CreateBackupAsync();
                }
                _notificationService.ShowSuccess("Database backup completed successfully.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Backup failed: {ex.Message}");
            }
        }

        // ── Save Security Settings ────────────────────────────────────────────

        private async void SaveSecurity_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;

            if (!int.TryParse(IdleTimeoutBox.Text, out var idleMinutes) || idleMinutes < 0)
            {
                _notificationService.ShowError("Auto-logout minutes must be a non-negative whole number.");
                return;
            }

            if (!int.TryParse(SmtpPortBox.Text, out var smtpPort) || smtpPort <= 0)
            {
                _notificationService.ShowError("SMTP port must be a positive whole number.");
                return;
            }

            _current.IdleTimeoutMinutes = idleMinutes;
            _current.SmtpHost = string.IsNullOrWhiteSpace(SmtpHostBox.Text) ? null : SmtpHostBox.Text.Trim();
            _current.SmtpPort = smtpPort;
            _current.SmtpUseSsl = SmtpUseSslCheck.IsChecked.GetValueOrDefault();
            _current.SmtpUsername = string.IsNullOrWhiteSpace(SmtpUsernameBox.Text) ? null : SmtpUsernameBox.Text.Trim();
            if (!string.IsNullOrEmpty(SmtpPasswordBox.Password))
                _current.SmtpPassword = SmtpPasswordBox.Password;
            _current.SmtpFromAddress = string.IsNullOrWhiteSpace(SmtpFromBox.Text) ? null : SmtpFromBox.Text.Trim();

            await Save();
        }

        private async void SendTestEmail_Click(object sender, RoutedEventArgs e)
        {
            var toAddress = TestEmailToBox.Text.Trim();
            if (string.IsNullOrEmpty(toAddress))
            {
                _notificationService.ShowError("Enter an address to send the test email to.");
                return;
            }

            if (!int.TryParse(SmtpPortBox.Text, out var smtpPort) || smtpPort <= 0)
            {
                _notificationService.ShowError("SMTP port must be a positive whole number.");
                return;
            }

            // Uses whatever is currently typed into the form, so an admin can verify
            // SMTP settings before saving them.
            var testSettings = new SystemSetting
            {
                SmtpHost = string.IsNullOrWhiteSpace(SmtpHostBox.Text) ? null : SmtpHostBox.Text.Trim(),
                SmtpPort = smtpPort,
                SmtpUseSsl = SmtpUseSslCheck.IsChecked.GetValueOrDefault(),
                SmtpUsername = string.IsNullOrWhiteSpace(SmtpUsernameBox.Text) ? null : SmtpUsernameBox.Text.Trim(),
                SmtpPassword = string.IsNullOrEmpty(SmtpPasswordBox.Password) ? _current?.SmtpPassword : SmtpPasswordBox.Password,
                SmtpFromAddress = string.IsNullOrWhiteSpace(SmtpFromBox.Text) ? null : SmtpFromBox.Text.Trim()
            };

            try
            {
                SendTestEmailButton.IsEnabled = false;
                SendTestEmailButton.Content = "Sending...";

                using (var scope = App.CreateDbScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await emailService.SendEmailAsync(
                        toAddress,
                        "HotelPOS test email",
                        "This is a test email from HotelPOS to confirm your SMTP settings are working.",
                        testSettings);
                }

                _notificationService.ShowSuccess($"Test email sent to {toAddress}.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Could not send test email: {ex.Message}");
            }
            finally
            {
                SendTestEmailButton.IsEnabled = true;
                SendTestEmailButton.Content = "✉  Send Test Email";
            }
        }

        private async void RestoreDb_Click(object sender, RoutedEventArgs e)
        {
            var confirm = await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessageAsync(
                "⚠️ WARNING: Restoring the database will overwrite all current data and reset active tables.\n\nAre you sure you want to continue?",
                "Confirm Database Restore",
                HotelPOS.Application.Interfaces.DialogButton.YesNo,
                HotelPOS.Application.Interfaces.DialogIcon.Warning);

            if (confirm != HotelPOS.Application.Interfaces.DialogResult.Yes) return;

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Backup Files (*.db;*.bak)|*.db;*.bak|All Files (*.*)|*.*",
                Title = "Select Backup File to Restore"
            };

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                try
                {
                    using (var scope = App.CreateDbScope())
                    {
                        var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
                        await backup.RestoreBackupAsync(dlg.FileName);
                    }
                    await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessageAsync(
                        "Database restored successfully!\n\nThe application will now close to reload context. Please restart the application.",
                        "Restore Success",
                        HotelPOS.Application.Interfaces.DialogButton.OK,
                        HotelPOS.Application.Interfaces.DialogIcon.Information);

                    System.Windows.Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Restore failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Persists the currently loaded settings and displays a success or error notification to the user.
        /// </summary>
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
