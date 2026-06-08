using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    // ── ViewModel wrapper for RolePermission ───────────────────────────────────
    // Provides icon, friendly display name, and description for each module.
    public class PermissionViewModel : INotifyPropertyChanged
    {
        private bool _canAccess;

        public int Id { get; set; }
        public int RoleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = "📄";
        public string Description { get; set; } = string.Empty;

        public bool CanAccess
        {
            get => _canAccess;
            set { _canAccess = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── Friendly metadata lookup ──────────────────────────────────────────
        private static readonly Dictionary<string, (string Icon, string Display, string Desc)> _meta = new()
        {
            ["Dashboard"]   = ("📊", "Dashboard",      "View sales summaries and KPI metrics"),
            ["Billing"]     = ("🖥",  "Billing POS",    "Create and manage customer bills"),
            ["Items"]       = ("📋", "Items / Menu",    "Add, edit, or delete menu items"),
            ["Categories"]  = ("🏷",  "Categories",     "Manage item category groups"),
            ["Tables"]      = ("🪑", "Tables",          "View and manage dining tables"),
            ["Ledger"]      = ("📒", "Ledger",          "View financial transaction ledger"),
            ["Journal"]     = ("📓", "Journal",         "View daily accounting journal entries"),
            ["Settings"]    = ("⚙",  "Settings",        "Configure system-wide settings"),
            ["Audit"]       = ("🛡",  "Audit Log",       "View system activity and audit trail"),
            ["Shift"]       = ("💵", "Shift / Session", "Open and close cash sessions"),
            ["Roles"]       = ("👥", "Roles",           "Manage user roles and permissions"),
            ["SalesReport"] = ("📈", "Sales Report",    "View and export detailed sales reports"),
        };

        public static PermissionViewModel FromPermission(RolePermission p)
        {
            _meta.TryGetValue(p.ModuleName, out var m);
            return new PermissionViewModel
            {
                Id          = p.Id,
                RoleId      = p.RoleId,
                ModuleName  = p.ModuleName,
                DisplayName = m.Display ?? p.ModuleName,
                Icon        = m.Icon ?? "📄",
                Description = m.Desc ?? "Toggle access to this module",
                CanAccess   = p.CanAccess,
            };
        }

        public RolePermission ToPermission() => new()
        {
            Id         = Id,
            RoleId     = RoleId,
            ModuleName = ModuleName,
            CanAccess  = CanAccess,
        };
    }

    // ── RolesView ─────────────────────────────────────────────────────────────
    public partial class RolesView : UserControl
    {
        private readonly INotificationService _notificationService;
        private Role? _selectedRole;
        private List<PermissionViewModel> _currentPermissions = new();

        /// <summary>
        /// Initializes the RolesView, wires required services, and begins loading role data when the control is loaded.
        /// </summary>
        /// <param name="roleService">Injected role service (resolved per operation) required for dependency injection.</param>
        /// <param name="notificationService">Service used to display notifications to the user.</param>
        public RolesView(IRoleService roleService, INotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        /// <summary>
        /// Loads roles from the role service, sets them as the ItemsSource for RolesGrid, and selects the "Admin" role if present (otherwise selects the first role).
        /// </summary>
        /// <returns>A task that completes after roles have been loaded and the RolesGrid selection has been updated.</returns>

        private async Task LoadDataAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
                var roles = await roleService.GetAllRolesAsync();
                RolesGrid.ItemsSource = roles;

                if (roles != null && roles.Any())
                {
                    var adminRole = roles.FirstOrDefault(r => r.Name == "Admin");
                    RolesGrid.SelectedItem = adminRole ?? roles.First();
                }
            }
        }

        // ── Selection ─────────────────────────────────────────────────────────

        private void RolesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRole = RolesGrid.SelectedItem as Role;

            if (_selectedRole != null)
            {
                EditingRoleTitle.Text = $"Permissions — {_selectedRole.Name}";

                // Deduplicate, prioritising Allow=true, then sort by display name
                _currentPermissions = _selectedRole.Permissions
                    .OrderByDescending(p => p.CanAccess)
                    .GroupBy(p => p.ModuleName)
                    .Select(g => PermissionViewModel.FromPermission(g.First()))
                    .OrderBy(p => p.DisplayName)
                    .ToList();

                PermissionsList.ItemsSource = _currentPermissions;

                // Admin role: disable delete button
                DeleteRoleBtn.IsEnabled = _selectedRole.Name != "Admin";
                DeleteRoleBtn.Opacity   = _selectedRole.Name == "Admin" ? 0.4 : 1.0;

                // Show editor, hide placeholder
                NoRolePlaceholder.Visibility = Visibility.Collapsed;
                PermissionEditor.Visibility  = Visibility.Visible;
            }
            else
            {
                NoRolePlaceholder.Visibility = Visibility.Visible;
                PermissionEditor.Visibility  = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Creates a new role using the name entered in the NewRoleNameBox, notifies the user of the result, and reloads the role list on success.
        /// </summary>
        /// <remarks>
        /// If the name input is empty no action is taken. The method resolves an <c>IRoleService</c> from a scoped service provider to perform the creation. On success it clears the input box, shows a success notification, and refreshes the displayed roles; on failure it shows an error notification.
        /// </remarks>

        private async void AddRole_Click(object sender, RoutedEventArgs e)
        {
            var name = NewRoleNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            bool success = false;
            using (var scope = App.CreateDbScope())
            {
                var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
                success = await roleService.AddRoleAsync(name, "");
            }

            if (success)
            {
                _notificationService.ShowSuccess($"Role '{name}' created.");
                NewRoleNameBox.Clear();
                await LoadDataAsync();
            }
            else
            {
                _notificationService.ShowError("Role already exists or an error occurred.");
            }
        }

        // ── Grant All / Revoke All ─────────────────────────────────────────────

        private void GrantAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in _currentPermissions) p.CanAccess = true;
        }

        private void RevokeAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in _currentPermissions) p.CanAccess = false;
        }

        /// <summary>
        /// Persist the edited permissions for the currently selected role and, if that role belongs to the active user, apply the changes to the running session's UI immediately.
        /// </summary>
        /// <remarks>
        /// Converts the view-model permission edits into domain permissions, updates them via the role service, and shows a success notification.
        /// If the saved role matches the current session user's role, the user's in-memory permissions are replaced and the dashboard's permission application is invoked so changes take effect without requiring re-login.
        /// </remarks>

        private async void SavePermissions_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null) return;

            var permissions = _currentPermissions
                .Select(vm => vm.ToPermission())
                .ToList();

            using (var scope = App.CreateDbScope())
            {
                var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
                await roleService.UpdateRolePermissionsAsync(_selectedRole.Id, permissions);
            }

            // ── Live-refresh: if the saved role is the current user's own role,
            //    update their in-memory permissions and re-apply sidebar visibility
            //    immediately — no re-login required for the active session.
            var currentUser = AppSession.CurrentUser;
            if (currentUser != null && currentUser.RoleId == _selectedRole.Id)
            {
                // Patch in-memory permissions so ApplyPermissions reads the new values
                if (currentUser.RoleDetails != null)
                {
                    currentUser.RoleDetails.Permissions = permissions;
                }

                // Re-apply sidebar nav visibility right now
                if (Window.GetWindow(this) is DashboardWindow dashboard)
                {
                    dashboard.ApplyPermissions();
                }

                _notificationService.ShowSuccess(
                    $"Permissions for '{_selectedRole.Name}' saved and applied to your current session.");
            }
            else
            {
                _notificationService.ShowSuccess(
                    $"Permissions for '{_selectedRole.Name}' saved. " +
                    "Users with this role will see changes after their next login.");
            }
        }

        /// <summary>
        /// Handles the Delete Role button click by confirming and removing the selected role, then updating the UI.
        /// </summary>
        /// <remarks>
        /// If the selected role is null the method exits immediately. Deletion is blocked for the "Admin" role and will display an error notification. When confirmed by the user, the role is removed via the role service, a success notification is shown, the current selection and permissions are cleared, the permission editor is hidden, and the roles list is reloaded.
        /// </remarks>

        private async void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null) return;

            if (_selectedRole.Name == "Admin")
            {
                _notificationService.ShowError("Cannot delete the Admin role.");
                return;
            }

            if (MessageBox.Show(
                    $"Delete role '{_selectedRole.Name}'?\nUsers assigned this role will lose all access.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using (var scope = App.CreateDbScope())
                {
                    var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
                    await roleService.DeleteRoleAsync(_selectedRole.Id);
                }

                _notificationService.ShowSuccess($"Role '{_selectedRole.Name}' deleted.");
                _selectedRole = null;
                _currentPermissions.Clear();
                NoRolePlaceholder.Visibility = Visibility.Visible;
                PermissionEditor.Visibility  = Visibility.Collapsed;
                await LoadDataAsync();
            }
        }
    }
}
