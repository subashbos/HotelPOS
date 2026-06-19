using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Views;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private TableView? _cachedTables;
        private AuditView? _cachedAudit;
        private SessionView? _cachedShift;
        private RolesView? _cachedRoles;
        private SalesReportView? _cachedSales;
        private BIReportView? _cachedBIReport;
        private ItemReportView? _cachedItemReport;
        private PurchaseReportView? _cachedPurchaseReport;
        private PurchaseEntryView? _cachedPurchase;
        private SupplierView? _cachedSuppliers;
        private readonly IThemeService _themeService;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly IRoleService _roleService;

        public DashboardWindow(IServiceProvider serviceProvider, IThemeService themeService,
            INotificationService notificationService, IUserRepository userRepository,
            IRoleService roleService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _themeService = themeService;
            _notificationService = notificationService;
            _userRepository = userRepository;
            _roleService = roleService;
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _themeService.ToggleTheme();
        }

        /// <summary>
        /// Initializes the window UI for the current session: updates user header, refreshes role permissions from the database, applies navigation permissions, and selects the first permitted module to show.
        /// </summary>
        /// <param name="sender">Event source (window).</param>
        /// <param name="e">Event arguments.</param>
        /// <remarks>
        /// If refreshing permissions from the database fails, the method silently falls back to the session's existing permission snapshot.
        /// </remarks>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSession.CurrentUser != null)
            {
                var username = AppSession.CurrentUser.Username;
                var role = AppSession.CurrentUser.Role;

                UserNameTextExp.Text = username;
                UserRoleText.Text = role;

                // Avatar initial
                var initial = username.Length > 0 ? username[0].ToString().ToUpper() : "U";
                UserAvatarTextExp.Text = initial;

                // ── Always refresh permissions from DB on load ────────────────
                // This ensures any permission changes made since the last login
                // (e.g. Admin granting Tables to Cashier mid-session) take effect
                // immediately without requiring the user to log out and back in.
                try
                {
                    // Step 1: Reload user from DB to get fresh RoleDetails + Permissions
                    User? freshUser = null;
                    using (var scope = App.CreateDbScope())
                    {
                        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                        freshUser = await userRepository.GetUserByUsernameAsync(
                            AppSession.CurrentUser.Username);
                    }

                    if (freshUser?.RoleDetails != null)
                    {
                        // Happy path: user has a linked RoleId with permissions
                        AppSession.CurrentUser.RoleDetails = freshUser.RoleDetails;
                    }
                    else if (!string.IsNullOrEmpty(AppSession.CurrentUser.Role))
                    {
                        Role? roleByName = null;
                        using (var scope = App.CreateDbScope())
                        {
                            var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
                            var roles = await roleService.GetAllRolesAsync();
                            roleByName = roles.FirstOrDefault(r => string.Equals(r.Name,
                                AppSession.CurrentUser.Role,
                                StringComparison.OrdinalIgnoreCase));
                        }

                        if (roleByName != null)
                        {
                            AppSession.CurrentUser.RoleDetails = roleByName;
                        }
                    }
                }
                catch { /* Non-critical: fall back to login-time snapshot */ }

                ApplyPermissions();
            }

            // Default to first available module based on permissions
            if (NavDash.Visibility == Visibility.Visible) NavDash_Click(null!, null!);
            else if (NavBilling.Visibility == Visibility.Visible) NavBilling_Click(null!, null!);
            else if (NavShift.Visibility == Visibility.Visible) NavShift_Click(null!, null!);
            else if (NavSales.Visibility == Visibility.Visible) NavSales_Click(null!, null!);
        }

        /// <summary>
        /// Re-evaluates sidebar visibility against the current session's permissions.
        /// Called once at login and again whenever the active user's role permissions are updated live.
        /// </summary>
        private bool HasPermission(string moduleName)
        {
            var user = AppSession.CurrentUser;
            if (user == null) return false;

            var permissions = user.RoleDetails?.Permissions ?? new List<RolePermission>();
            var perm = permissions.FirstOrDefault(p => string.Equals(p.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));

            if (perm != null)
            {
                return perm.CanAccess;
            }

            // Fallback
            bool isAdmin = string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isCashier = string.Equals(user.Role, "Cashier", StringComparison.OrdinalIgnoreCase);

            if (isAdmin) return true;
            if (isCashier && (moduleName == "Billing" || moduleName == "Shift")) return true;

            return false;
        }

        /// <summary>
        /// Re-evaluates sidebar visibility against the current session's permissions.
        /// Called once at login and again whenever the active user's role permissions are updated live.
        /// </summary>
        public void ApplyPermissions()
        {
            var user = AppSession.CurrentUser;
            if (user == null) return;

            NavDash.Visibility = HasPermission("Dashboard") ? Visibility.Visible : Visibility.Collapsed;
            NavBIReport.Visibility = HasPermission("SalesReport") ? Visibility.Visible : Visibility.Collapsed;
            NavBilling.Visibility = HasPermission("Billing") ? Visibility.Visible : Visibility.Collapsed;

            NavSales.Visibility = HasPermission("SalesReport") ? Visibility.Visible : Visibility.Collapsed;
            NavShift.Visibility = HasPermission("Shift") ? Visibility.Visible : Visibility.Collapsed;

            NavMenu.Visibility = HasPermission("Items") ? Visibility.Visible : Visibility.Collapsed;
            NavTables.Visibility = HasPermission("Tables") ? Visibility.Visible : Visibility.Collapsed;

            NavCats.Visibility = HasPermission("Categories") ? Visibility.Visible : Visibility.Collapsed;
            NavPurchase.Visibility = HasPermission("Purchase") ? Visibility.Visible : Visibility.Collapsed;
            NavSuppliers.Visibility = HasPermission("Purchase") ? Visibility.Visible : Visibility.Collapsed;

            NavItemReport.Visibility = HasPermission("SalesReport") ? Visibility.Visible : Visibility.Collapsed;
            NavPurchaseReport.Visibility = HasPermission("Purchase") || HasPermission("SalesReport") ? Visibility.Visible : Visibility.Collapsed;
            NavLedger.Visibility = HasPermission("Ledger") ? Visibility.Visible : Visibility.Collapsed;
            NavJournal.Visibility = HasPermission("Journal") ? Visibility.Visible : Visibility.Collapsed;

            NavRoles.Visibility = HasPermission("Roles") ? Visibility.Visible : Visibility.Collapsed;

            NavSettings.Visibility = HasPermission("Settings") ? Visibility.Visible : Visibility.Collapsed;
            NavAudit.Visibility = HasPermission("Audit") ? Visibility.Visible : Visibility.Collapsed;

            // Update section header visibilities dynamically
            UpdateHeaderVisibilities();
        }

        // ── Navigation ────────────────────────────────────────────────────────

        private void NavDash_Click(object sender, RoutedEventArgs e)
        {
            _cachedDash ??= _serviceProvider.GetRequiredService<DashboardView>();
            MainContentArea.Content = _cachedDash;
            SetActive(NavDash);
        }

        private void NavBIReport_Click(object sender, RoutedEventArgs e)
        {
            _cachedBIReport ??= _serviceProvider.GetRequiredService<BIReportView>();
            MainContentArea.Content = _cachedBIReport;
            SetActive(NavBIReport);
            _ = _cachedBIReport.LoadDataAsync();
        }

        private void NavBilling_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cachedBilling ??= _serviceProvider.GetRequiredService<BillingView>();
                MainContentArea.Content = _cachedBilling;
                SetActive(NavBilling);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to open Billing POS: {ex.Message}");
                Serilog.Log.Error(ex, "Failed to resolve or open BillingView");
            }
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

        private void NavTables_Click(object sender, RoutedEventArgs e)
        {
            _cachedTables ??= _serviceProvider.GetRequiredService<TableView>();
            MainContentArea.Content = _cachedTables;
            SetActive(NavTables);
        }

        private void NavPurchase_Click(object sender, RoutedEventArgs e)
        {
            _cachedPurchase ??= _serviceProvider.GetRequiredService<PurchaseEntryView>();
            MainContentArea.Content = _cachedPurchase;
            SetActive(NavPurchase);
        }

        private void NavSuppliers_Click(object sender, RoutedEventArgs e)
        {
            _cachedSuppliers ??= _serviceProvider.GetRequiredService<SupplierView>();
            MainContentArea.Content = _cachedSuppliers;
            SetActive(NavSuppliers);
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

        private void NavSales_Click(object sender, RoutedEventArgs e)
        {
            _cachedSales ??= _serviceProvider.GetRequiredService<SalesReportView>();
            MainContentArea.Content = _cachedSales;
            SetActive(NavSales);
        }

        private void NavItemReport_Click(object sender, RoutedEventArgs e)
        {
            _cachedItemReport ??= _serviceProvider.GetRequiredService<ItemReportView>();
            MainContentArea.Content = _cachedItemReport;
            SetActive(NavItemReport);
        }

        private void NavPurchaseReport_Click(object sender, RoutedEventArgs e)
        {
            _cachedPurchaseReport ??= _serviceProvider.GetRequiredService<PurchaseReportView>();
            MainContentArea.Content = _cachedPurchaseReport;
            SetActive(NavPurchaseReport);
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

        private void NavRoles_Click(object sender, RoutedEventArgs e)
        {
            _cachedRoles ??= _serviceProvider.GetRequiredService<RolesView>();
            MainContentArea.Content = _cachedRoles;
            SetActive(NavRoles);
        }

        private void SetActive(Button active)
        {
            // 1. Enable all 13 navigation buttons
            var allButtons = new[]
            {
                NavBilling, NavShift,
                NavMenu, NavCats, NavTables, NavPurchase, NavSuppliers,
                NavDash, NavBIReport, NavSales, NavItemReport, NavPurchaseReport, NavLedger, NavJournal,
                NavSettings, NavRoles, NavAudit
            };
            foreach (var btn in allButtons)
            {
                btn.IsEnabled = true;
            }

            // 2. Disable the active button to trigger its visual teal/cyan active state template
            active.IsEnabled = false;

            // 3. Use ToolTip or Tag as the page title
            PageTitleText.Text = active.ToolTip?.ToString() ?? active.Tag?.ToString() ?? "Dashboard";

            // 4. Ensure header visibilities are up-to-date
            UpdateHeaderVisibilities();
        }

        private void UpdateHeaderVisibilities()
        {
            bool isExpanded = (string?)SidebarBorder.Tag == "expanded";

            if (!isExpanded)
            {
                // Compact mode: hide all headers
                HeaderOps.Visibility = Visibility.Collapsed;
                HeaderInv.Visibility = Visibility.Collapsed;
                HeaderStats.Visibility = Visibility.Collapsed;
                HeaderAdmin.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Expanded mode: show header ONLY if at least one child button is visible
                HeaderOps.Visibility = (NavBilling.Visibility == Visibility.Visible || NavShift.Visibility == Visibility.Visible)
                    ? Visibility.Visible : Visibility.Collapsed;

                HeaderInv.Visibility = (NavMenu.Visibility == Visibility.Visible || NavCats.Visibility == Visibility.Visible || NavTables.Visibility == Visibility.Visible || NavPurchase.Visibility == Visibility.Visible || NavSuppliers.Visibility == Visibility.Visible)
                    ? Visibility.Visible : Visibility.Collapsed;

                HeaderStats.Visibility = (NavDash.Visibility == Visibility.Visible || NavSales.Visibility == Visibility.Visible || NavItemReport.Visibility == Visibility.Visible || NavPurchaseReport.Visibility == Visibility.Visible || NavLedger.Visibility == Visibility.Visible || NavJournal.Visibility == Visibility.Visible)
                    ? Visibility.Visible : Visibility.Collapsed;

                HeaderAdmin.Visibility = (NavSettings.Visibility == Visibility.Visible || NavRoles.Visibility == Visibility.Visible || NavAudit.Visibility == Visibility.Visible)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ── Sidebar Toggle ────────────────────────────────────────────────────

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            bool expand = (string?)SidebarBorder.Tag != "expanded";

            SidebarBorder.Tag = expand ? "expanded" : "compact";
            double targetWidth = expand ? 220 : 70;

            // Smooth hardware-accelerated grid sidebar animation
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = targetWidth,
                Duration = System.TimeSpan.FromMilliseconds(200),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };
            SidebarBorder.BeginAnimation(WidthProperty, animation);

            // Bottom panels
            BottomCompact.Visibility = expand ? Visibility.Collapsed : Visibility.Visible;
            BottomExpanded.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;

            // Header panels
            HeaderCompact.Visibility = expand ? Visibility.Collapsed : Visibility.Visible;
            HeaderExpanded.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;

            // Update section header visibilities dynamically
            UpdateHeaderVisibilities();
        }

        public void StartEditOrder(Order order)
        {
            // Switch to Billing view
            NavBilling_Click(null!, null!);

            // Pass order to BillingView
            if (MainContentArea.Content is BillingView bv)
            {
                bv.LoadOrderForEdit(order);

                // One-shot: when the update completes, navigate back to Sales Report and refresh
                Action? handler = null;
                handler = () =>
                {
                    bv.OrderUpdated -= handler;
                    NavSales_Click(null!, null!);
                    if (_cachedSales != null)
                        _ = _cachedSales.LoadDataAsync();
                };
                bv.OrderUpdated += handler;

                // Handle Cancel
                Action? cancelHandler = null;
                cancelHandler = () =>
                {
                    bv.OrderEditCancelled -= cancelHandler;
                    NavSales_Click(null!, null!);
                };
                bv.OrderEditCancelled += cancelHandler;
            }
        }

        // ── Session ───────────────────────────────────────────────────────────

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cartService = _serviceProvider.GetRequiredService<ICartService>();
                cartService.ClearAll();
            }
            catch { }

            AppSession.Logout();
            Closing -= Window_Closing;  // skip confirm dialog on explicit logout
            Close();                    // → Closed event → scope.Dispose + ShowLoginWindow()
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (MessageBox.Show("Logout and close workspace?", "Exit",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var cartService = _serviceProvider.GetRequiredService<ICartService>();
                    cartService.ClearAll();
                }
                catch { }

                AppSession.Logout();
                // Close() fires → Closed event → scope.Dispose() + ShowLoginWindow()
            }
            else e.Cancel = true;
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Global shortcuts for Billing POS when it's active
            if (MainContentArea.Content is BillingView bv)
            {
                if (e.Key == Key.F1 || e.Key == Key.F3)
                {
                    if (!bv.IsKeyboardFocusWithin)
                    {
                        bv.FocusSearch();
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.F4)
                {
                    if (!bv.IsKeyboardFocusWithin)
                    {
                        bv.TriggerCheckout();
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
