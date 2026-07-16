using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Views;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace HotelPOS
{
    public partial class DashboardWindow : Window
    {
        private const string PurchaseModule = "Purchase";

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
        private UsersView? _cachedUsers;
        private SalesReportView? _cachedSales;
        private BIReportView? _cachedBIReport;
        private ItemReportView? _cachedItemReport;
        private PurchaseReportView? _cachedPurchaseReport;
        private PurchaseEntryView? _cachedPurchase;
        private SupplierView? _cachedSuppliers;
        private RawMaterialView? _cachedRawMaterials;
        private BomView? _cachedBom;
        private EmployeeView? _cachedEmployees;
        private AttendanceView? _cachedAttendance;
        private LeaveView? _cachedLeave;
        private PayrollView? _cachedPayroll;
        private ExpenseView? _cachedExpenses;
        private readonly IThemeService _themeService;
        private readonly INotificationService _notificationService;

        // ── Idle timeout ──────────────────────────────────────────────────────
        private DispatcherTimer? _idleTimer;
        private DateTime _lastActivityUtc = DateTime.UtcNow;
        private TimeSpan _idleTimeout = TimeSpan.Zero;

        public DashboardWindow(IServiceProvider serviceProvider, IThemeService themeService,
            INotificationService notificationService, IUserRepository userRepository,
            IRoleService roleService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _themeService = themeService;
            _notificationService = notificationService;
            Closed += DashboardWindow_Closed;
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

            await InitializeIdleTimeoutAsync();
        }

        // ── Idle timeout ──────────────────────────────────────────────────────

        private async Task InitializeIdleTimeoutAsync()
        {
            try
            {
                int minutes;
                using (var scope = App.CreateDbScope())
                {
                    var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                    var settings = await settingService.GetSettingsAsync();
                    minutes = settings.IdleTimeoutMinutes;
                }

                if (minutes <= 0) return; // disabled

                _idleTimeout = TimeSpan.FromMinutes(minutes);
                _lastActivityUtc = DateTime.UtcNow;
                InputManager.Current.PreProcessInput += InputManager_PreProcessInput;

                _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
                _idleTimer.Tick += IdleTimer_Tick;
                _idleTimer.Start();
            }
            catch { /* Idle timeout is a convenience feature; never block dashboard load */ }
        }

        private void InputManager_PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            _lastActivityUtc = DateTime.UtcNow;
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            if (DateTime.UtcNow - _lastActivityUtc >= _idleTimeout)
            {
                _idleTimer?.Stop();
                AutoLogoutForInactivity();
            }
        }

        private void AutoLogoutForInactivity()
        {
            ClearCartBestEffort();
            LogLogoutAudit("Idle timeout");
            AppSession.Logout();
            Closing -= Window_Closing; // skip confirm dialog on auto-logout
            Close();
        }

        private void DashboardWindow_Closed(object? sender, EventArgs e)
        {
            InputManager.Current.PreProcessInput -= InputManager_PreProcessInput;
            _idleTimer?.Stop();
        }

        /// <summary>
        /// Re-evaluates sidebar visibility against the current session's permissions.
        /// Called once at login and again whenever the active user's role permissions are updated live.
        /// </summary>
        private static bool HasPermission(string moduleName)
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
            bool isAdmin = string.Equals(user.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);
            bool isCashier = string.Equals(user.Role, RoleNames.Cashier, StringComparison.OrdinalIgnoreCase);

            if (isAdmin) return true;
            if (isCashier && (moduleName == PermissionModules.Billing || moduleName == PermissionModules.Shift)) return true;

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

            NavDash.Visibility = Vis(HasPermission("Dashboard"));
            NavBIReport.Visibility = Vis(HasPermission(PermissionModules.SalesReport));
            NavBilling.Visibility = Vis(HasPermission("Billing"));

            NavSales.Visibility = Vis(HasPermission(PermissionModules.SalesReport));
            NavShift.Visibility = Vis(HasPermission("Shift"));
            NavExpenses.Visibility = Vis(HasPermission(PermissionModules.Expenses));

            NavMenu.Visibility = Vis(HasPermission("Items"));
            NavTables.Visibility = Vis(HasPermission("Tables"));

            NavCats.Visibility = Vis(HasPermission("Categories"));
            NavPurchase.Visibility = Vis(HasPermission(PurchaseModule));
            NavSuppliers.Visibility = Vis(HasPermission(PurchaseModule));
            NavRawMaterials.Visibility = Vis(HasPermission(PurchaseModule));
            NavBom.Visibility = Vis(HasPermission(PurchaseModule));

            NavItemReport.Visibility = Vis(HasPermission(PermissionModules.SalesReport));
            NavPurchaseReport.Visibility = Vis(HasPermission(PurchaseModule) || HasPermission(PermissionModules.SalesReport));
            NavLedger.Visibility = Vis(HasPermission("Ledger"));
            NavJournal.Visibility = Vis(HasPermission("Journal"));

            NavRoles.Visibility = Vis(HasPermission("Roles"));
            NavUsers.Visibility = Vis(HasPermission("Settings"));

            NavEmployees.Visibility = Vis(HasPermission(PermissionModules.HrEmployees));
            NavAttendance.Visibility = Vis(HasPermission(PermissionModules.HrAttendance));
            NavLeave.Visibility = Vis(HasPermission(PermissionModules.HrLeave));
            NavPayroll.Visibility = Vis(HasPermission(PermissionModules.HrPayroll));

            NavSettings.Visibility = Vis(HasPermission("Settings"));
            NavAudit.Visibility = Vis(HasPermission("Audit"));

            // Update section header visibilities dynamically
            UpdateHeaderVisibilities();
        }

        private static Visibility Vis(bool allowed) => allowed ? Visibility.Visible : Visibility.Collapsed;

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

        private void NavRawMaterials_Click(object sender, RoutedEventArgs e)
        {
            _cachedRawMaterials ??= _serviceProvider.GetRequiredService<RawMaterialView>();
            MainContentArea.Content = _cachedRawMaterials;
            SetActive(NavRawMaterials);
        }

        private void NavBom_Click(object sender, RoutedEventArgs e)
        {
            _cachedBom ??= _serviceProvider.GetRequiredService<BomView>();
            MainContentArea.Content = _cachedBom;
            SetActive(NavBom);
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

        private void NavExpenses_Click(object sender, RoutedEventArgs e)
        {
            _cachedExpenses ??= _serviceProvider.GetRequiredService<ExpenseView>();
            MainContentArea.Content = _cachedExpenses;
            SetActive(NavExpenses);
        }

        private void NavRoles_Click(object sender, RoutedEventArgs e)
        {
            _cachedRoles ??= _serviceProvider.GetRequiredService<RolesView>();
            MainContentArea.Content = _cachedRoles;
            SetActive(NavRoles);
        }

        private void NavUsers_Click(object sender, RoutedEventArgs e)
        {
            _cachedUsers ??= _serviceProvider.GetRequiredService<UsersView>();
            MainContentArea.Content = _cachedUsers;
            SetActive(NavUsers);
        }

        private void NavEmployees_Click(object sender, RoutedEventArgs e)
        {
            _cachedEmployees ??= _serviceProvider.GetRequiredService<EmployeeView>();
            MainContentArea.Content = _cachedEmployees;
            SetActive(NavEmployees);
        }

        private void NavAttendance_Click(object sender, RoutedEventArgs e)
        {
            _cachedAttendance ??= _serviceProvider.GetRequiredService<AttendanceView>();
            MainContentArea.Content = _cachedAttendance;
            SetActive(NavAttendance);
        }

        private void NavLeave_Click(object sender, RoutedEventArgs e)
        {
            _cachedLeave ??= _serviceProvider.GetRequiredService<LeaveView>();
            MainContentArea.Content = _cachedLeave;
            SetActive(NavLeave);
        }

        private void NavPayroll_Click(object sender, RoutedEventArgs e)
        {
            _cachedPayroll ??= _serviceProvider.GetRequiredService<PayrollView>();
            MainContentArea.Content = _cachedPayroll;
            SetActive(NavPayroll);
        }

        private void SetActive(Button active)
        {
            // 1. Enable all navigation buttons
            var allButtons = new[]
            {
                NavBilling, NavShift, NavExpenses,
                NavMenu, NavCats, NavTables, NavPurchase, NavSuppliers, NavRawMaterials, NavBom,
                NavDash, NavBIReport, NavSales, NavItemReport, NavPurchaseReport, NavLedger, NavJournal,
                NavEmployees, NavAttendance, NavLeave, NavPayroll,
                NavSettings, NavRoles, NavUsers, NavAudit
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

        private void UpdateHeaderVisibilities() // NOSONAR
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
                HeaderOps.Visibility = (NavBilling.Visibility == Visibility.Visible || NavShift.Visibility == Visibility.Visible || NavExpenses.Visibility == Visibility.Visible)
                    ? Visibility.Visible : Visibility.Collapsed;

                HeaderInv.Visibility = (NavMenu.Visibility == Visibility.Visible || NavCats.Visibility == Visibility.Visible || NavTables.Visibility == Visibility.Visible || NavPurchase.Visibility == Visibility.Visible || NavSuppliers.Visibility == Visibility.Visible || NavRawMaterials.Visibility == Visibility.Visible || NavBom.Visibility == Visibility.Visible)
                    ? Visibility.Visible : Visibility.Collapsed;

                HeaderStats.Visibility = (NavDash.Visibility == Visibility.Visible || NavSales.Visibility == Visibility.Visible || NavItemReport.Visibility == Visibility.Visible || NavPurchaseReport.Visibility == Visibility.Visible || NavLedger.Visibility == Visibility.Visible || NavJournal.Visibility == Visibility.Visible)
                    ? Visibility.Visible : Visibility.Collapsed;

                HeaderAdmin.Visibility = (NavSettings.Visibility == Visibility.Visible || NavRoles.Visibility == Visibility.Visible || NavUsers.Visibility == Visibility.Visible || NavAudit.Visibility == Visibility.Visible)
                    ? Visibility.Visible : Visibility.Collapsed;

                HeaderHR.Visibility = (NavEmployees.Visibility == Visibility.Visible || NavAttendance.Visibility == Visibility.Visible
                        || NavLeave.Visibility == Visibility.Visible || NavPayroll.Visibility == Visibility.Visible)
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
            ClearCartBestEffort();
            LogLogoutAudit();
            ClearRememberMe();
            AppSession.Logout();
            Closing -= Window_Closing;  // skip confirm dialog on explicit logout
            Close();                    // → Closed event → scope.Dispose + ShowLoginWindow()
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessage("Logout and close workspace?", "Exit",
                HotelPOS.Application.Interfaces.DialogButton.YesNo, HotelPOS.Application.Interfaces.DialogIcon.Question) == HotelPOS.Application.Interfaces.DialogResult.Yes)
            {
                ClearCartBestEffort();
                LogLogoutAudit();
                ClearRememberMe();
                AppSession.Logout();
                // Close() fires → Closed event → scope.Dispose() + ShowLoginWindow()
            }
            else e.Cancel = true;
        }

        /// <summary>Best-effort cart clear so a failure here never blocks logout.</summary>
        private void ClearCartBestEffort()
        {
            try
            {
                var cartService = _serviceProvider.GetRequiredService<ICartService>();
                cartService.ClearAll();
            }
            catch
            {
                // Best-effort: logout must proceed even if the in-memory cart couldn't be cleared.
            }
        }

        /// <summary>Best-effort audit entry for the current session's logout; never blocks the UI thread.</summary>
        private void LogLogoutAudit(string reason = "Manual logout")
        {
            var user = AppSession.CurrentUser;
            if (user == null) return;

            try
            {
                var auditService = _serviceProvider.GetRequiredService<IAuditService>();
                _ = auditService.LogActionAsync("User", user.Id, AuditActions.Logout, $"{reason}: {user.Username}");
            }
            catch
            {
                // Best-effort audit logging: logout must never be blocked by an audit-log failure.
            }
        }

        /// <summary>
        /// Revokes and deletes any saved "remember me" credential. Called only on an explicit sign-out —
        /// NOT on idle-timeout auto-logout, which is meant to be a session lock rather than a full sign-out.
        /// </summary>
        private void ClearRememberMe()
        {
            try
            {
                var saved = Services.RememberMeStore.Load();
                Services.RememberMeStore.Clear();
                if (saved != null)
                {
                    var rememberMeService = _serviceProvider.GetRequiredService<IRememberMeService>();
                    _ = rememberMeService.RevokeAsync(saved.Value.Token);
                }
            }
            catch
            {
                // Best-effort: sign-out must complete even if the remember-me token couldn't be revoked.
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) // NOSONAR
        {
            // Global shortcuts for Billing POS when it's active
            if (MainContentArea.Content is BillingView bv)
            {
                if ((e.Key == Key.F1 || e.Key == Key.F3) && !bv.IsKeyboardFocusWithin)
                {
                    bv.FocusSearch();
                    e.Handled = true;
                }
                else if (e.Key == Key.F4 && !bv.IsKeyboardFocusWithin)
                {
                    bv.TriggerCheckout();
                    e.Handled = true;
                }
            }
        }
    }
}
