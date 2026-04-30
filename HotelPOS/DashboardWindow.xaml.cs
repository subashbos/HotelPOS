using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.Views;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS
{
    public partial class DashboardWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        // Cached views — created once, reused on navigation
        private DashboardView? _cachedDash;
        private BillingView? _cachedBilling;
        private ItemView? _cachedMenu;
        private SettingsView? _cachedSettings;
        private LedgerView? _cachedLedger;
        private JournalView? _cachedJournal;
        private CategoryView? _cachedCats;
        private AuditView? _cachedAudit;
        private SessionView? _cachedShift;
        private readonly IThemeService _themeService;
        private readonly INotificationService _notificationService;

        public DashboardWindow(IServiceProvider serviceProvider, IThemeService themeService, INotificationService notificationService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _themeService = themeService;
            _notificationService = notificationService;

            _notificationService.NotificationReceived += (e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    NotificationPanel.Show(e.Message, e.Type);
                });
            };
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _themeService.ToggleTheme();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSession.CurrentUser != null)
            {
                UserNameText.Text = AppSession.CurrentUser.Username;
                UserRoleText.Text = AppSession.CurrentUser.Role;
            }

            if (AppSession.IsManager)
                NavDash_Click(null!, null!);
            else
            {
                // Cashiers: hide admin-only modules
                NavDash.Visibility = Visibility.Collapsed;
                NavMenu.Visibility = Visibility.Collapsed;
                NavLedger.Visibility = Visibility.Collapsed;
                NavJournal.Visibility = Visibility.Collapsed;
                NavSettings.Visibility = Visibility.Collapsed;
                NavCats.Visibility = Visibility.Collapsed;
                NavBilling_Click(null!, null!);
            }
        }

        // ── Navigation ────────────────────────────────────────────────────────

        private void NavDash_Click(object sender, RoutedEventArgs e)
        {
            _cachedDash ??= _serviceProvider.GetRequiredService<DashboardView>();
            MainContentArea.Content = _cachedDash;
            SetActive(NavDash);
        }

        private void NavBilling_Click(object sender, RoutedEventArgs e)
        {
            _cachedBilling ??= _serviceProvider.GetRequiredService<BillingView>();
            MainContentArea.Content = _cachedBilling;
            SetActive(NavBilling);
        }

        private void NavMenu_Click(object sender, RoutedEventArgs e)
        {
            _cachedMenu ??= _serviceProvider.GetRequiredService<ItemView>();
            MainContentArea.Content = _cachedMenu;
            SetActive(NavMenu);
        }

        private void NavCats_Click(object sender, RoutedEventArgs e)
        {
            _cachedCats ??= _serviceProvider.GetRequiredService<CategoryView>();
            MainContentArea.Content = _cachedCats;
            SetActive(NavCats);
        }

        private void NavLedger_Click(object sender, RoutedEventArgs e)
        {
            _cachedLedger ??= _serviceProvider.GetRequiredService<LedgerView>();
            MainContentArea.Content = _cachedLedger;
            SetActive(NavLedger);
        }

        private void NavJournal_Click(object sender, RoutedEventArgs e)
        {
            _cachedJournal ??= _serviceProvider.GetRequiredService<JournalView>();
            MainContentArea.Content = _cachedJournal;
            SetActive(NavJournal);
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            _cachedSettings ??= _serviceProvider.GetRequiredService<SettingsView>();
            MainContentArea.Content = _cachedSettings;
            SetActive(NavSettings);
        }

        private void NavAudit_Click(object sender, RoutedEventArgs e)
        {
            _cachedAudit ??= _serviceProvider.GetRequiredService<AuditView>();
            MainContentArea.Content = _cachedAudit;
            SetActive(NavAudit);
        }

        private void NavShift_Click(object sender, RoutedEventArgs e)
        {
            _cachedShift ??= _serviceProvider.GetRequiredService<SessionView>();
            MainContentArea.Content = _cachedShift;
            SetActive(NavShift);
        }

        private void SetActive(Button active)
        {
            foreach (var btn in new[] { NavDash, NavBilling, NavMenu, NavCats, NavLedger, NavJournal, NavSettings, NavAudit, NavShift })
                btn.IsEnabled = btn != active;

            // Update Header Title
            PageTitleText.Text = active.Content.ToString()?.Split(' ').Last() ?? "Dashboard";
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            if (SidebarColumn.Width.Value > 70)
            {
                SidebarColumn.Width = new GridLength(70);
                SidebarLogoArea.Visibility = Visibility.Collapsed;
                UserInfoGrid.Visibility = Visibility.Collapsed;
                
                // Hide text in nav buttons (keep only emojis/icons)
                foreach (var btn in new[] { NavDash, NavBilling, NavMenu, NavCats, NavLedger, NavJournal, NavSettings, NavAudit, NavShift })
                {
                    btn.Content = btn.Content.ToString()?.Split(' ').FirstOrDefault() ?? "";
                    btn.Padding = new Thickness(0, 12, 0, 12);
                    btn.HorizontalContentAlignment = HorizontalAlignment.Center;
                }
            }
            else
            {
                SidebarColumn.Width = new GridLength(260);
                SidebarLogoArea.Visibility = Visibility.Visible;
                UserInfoGrid.Visibility = Visibility.Visible;

                // Restore text in nav buttons
                NavDash.Content = "📊  Dashboard";
                NavBilling.Content = "🖥  Billing POS";
                NavMenu.Content = "📋  Items";
                NavCats.Content = "🏷  Categories";
                NavLedger.Content = "📒  Ledger";
                NavJournal.Content = "📓  Journal";
                NavSettings.Content = "⚙  Settings";
                NavAudit.Content = "🛡  Audit";
                NavShift.Content = "💵  Shift";

                foreach (var btn in new[] { NavDash, NavBilling, NavMenu, NavCats, NavLedger, NavJournal, NavSettings, NavAudit, NavShift })
                {
                    btn.Padding = new Thickness(20, 12, 20, 12);
                    btn.HorizontalContentAlignment = HorizontalAlignment.Left;
                }
            }
        }

        public void StartEditOrder(Order order)
        {
            // Switch to Billing view
            NavBilling_Click(null!, null!);

            // Pass order to BillingView
            if (MainContentArea.Content is BillingView bv)
            {
                bv.LoadOrderForEdit(order);
            }
        }

        // ── Session ───────────────────────────────────────────────────────────

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            AppSession.Logout();
            Closing -= Window_Closing;  // skip confirm dialog on explicit logout
            Close();                    // → Closed event → scope.Dispose + ShowLoginWindow()
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (MessageBox.Show("Logout and close workspace?", "Exit",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AppSession.Logout();
                // Close() fires → Closed event → scope.Dispose() + ShowLoginWindow()
            }
            else e.Cancel = true;
        }
    }
}
