using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
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
        private ItemReportView? _cachedItemReport;
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

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSession.CurrentUser != null)
            {
                UserNameText.Text = AppSession.CurrentUser.Username;
                UserRoleText.Text = AppSession.CurrentUser.Role;

                // ── Always refresh permissions from DB on load ────────────────
                // This ensures any permission changes made since the last login
                // (e.g. Admin granting Tables to Cashier mid-session) take effect
                // immediately without requiring the user to log out and back in.
                try
                {
                    // Step 1: Reload user from DB to get fresh RoleDetails + Permissions
                    var freshUser = await _userRepository.GetUserByUsernameAsync(
                        AppSession.CurrentUser.Username);

                    if (freshUser?.RoleDetails != null)
                    {
                        // Happy path: user has a linked RoleId with permissions
                        AppSession.CurrentUser.RoleDetails = freshUser.RoleDetails;
                    }
                    else if (!string.IsNullOrEmpty(AppSession.CurrentUser.Role))
                    {
                        // Fallback: RoleId is null on the user record — look up role by name
                        // This handles users created before role linking was enforced.
                        var roleByName = await _roleService
                            .GetAllRolesAsync()
                            .ContinueWith(t => t.Result
                                .FirstOrDefault(r => string.Equals(r.Name,
                                    AppSession.CurrentUser.Role,
                                    StringComparison.OrdinalIgnoreCase)));

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
        public void ApplyPermissions()
        {
            var user = AppSession.CurrentUser;
            if (user == null) return;

            // Ensure we have permissions. If RoleDetails is missing, the user might have been loaded
            // without Includes, so we fallback to role-based defaults for safety.
            var permissions = user.RoleDetails?.Permissions ?? new List<RolePermission>();

            // Helper to set visibility
            void SetVisibility(Expander module, Button btn, string moduleName)
            {
                // Find permission with case-insensitive matching
                var perm = permissions.FirstOrDefault(p => string.Equals(p.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));
                bool canAccess;
                
                if (perm != null)
                {
                    canAccess = perm.CanAccess;
                }
                else
                {
                    // Default fallback logic if permission record is missing
                    // Use case-insensitive check for role string as well
                    bool isAdmin = string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
                    bool isCashier = string.Equals(user.Role, "Cashier", StringComparison.OrdinalIgnoreCase);
                    
                    canAccess = isAdmin;
                    
                    // Basic modules accessible to Cashiers by default if record is missing
                    if (isCashier && (moduleName == "Billing" || moduleName == "Shift"))
                        canAccess = true;
                }

                btn.Visibility = canAccess ? Visibility.Visible : Visibility.Collapsed;

                // Update parent expander visibility: hide if all children are hidden
                if (module.Content is StackPanel sp)
                {
                    bool anyVisible = sp.Children.OfType<Button>().Any(b => b.Visibility == Visibility.Visible);
                    module.Visibility = anyVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            // Sync with RoleService.cs module list
            SetVisibility(ModuleStats, NavDash, "Dashboard");
            SetVisibility(ModuleOps, NavBilling, "Billing");
            SetVisibility(ModuleInv, NavMenu, "Items");
            SetVisibility(ModuleInv, NavCats, "Categories");
            SetVisibility(ModuleInv, NavTables, "Tables");
            SetVisibility(ModuleStats, NavLedger, "Ledger");
            SetVisibility(ModuleStats, NavJournal, "Journal");
            SetVisibility(ModuleAdmin, NavSettings, "Settings");
            SetVisibility(ModuleAdmin, NavAudit, "Audit");
            SetVisibility(ModuleOps, NavShift, "Shift");
            SetVisibility(ModuleAdmin, NavRoles, "Roles");
            SetVisibility(ModuleStats, NavSales, "SalesReport");
            SetVisibility(ModuleStats, NavItemReport, "SalesReport");
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

        private void NavTables_Click(object sender, RoutedEventArgs e)
        {
            _cachedTables ??= _serviceProvider.GetRequiredService<TableView>();
            MainContentArea.Content = _cachedTables;
            SetActive(NavTables);
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
            foreach (var btn in new[] { NavDash, NavBilling, NavMenu, NavCats, NavTables, NavLedger, NavJournal, NavSettings, NavAudit, NavShift, NavRoles, NavSales, NavItemReport })
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
                foreach (var btn in new[] { NavDash, NavBilling, NavMenu, NavCats, NavTables, NavLedger, NavJournal, NavSettings, NavAudit, NavShift, NavRoles, NavSales, NavItemReport })
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
                NavTables.Content = "🪑  Tables";
                NavLedger.Content = "📒  Ledger";
                NavJournal.Content = "📓  Journal";
                NavSettings.Content = "⚙  Settings";
                NavRoles.Content = "👥  Roles";
                NavAudit.Content = "🛡  Audit";
                NavShift.Content = "💵  Shift";
                NavSales.Content = "📈  Sales Report";
                NavItemReport.Content = "🍲  Item Report";

                foreach (var btn in new[] { NavDash, NavBilling, NavMenu, NavCats, NavTables, NavLedger, NavJournal, NavSettings, NavAudit, NavShift, NavRoles, NavSales, NavItemReport })
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
