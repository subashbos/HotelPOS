using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class RolesView : UserControl
    {
        private readonly IRoleService _roleService;
        private readonly INotificationService _notificationService;
        private Role? _selectedRole;

        public RolesView(IRoleService roleService, INotificationService notificationService)
        {
            InitializeComponent();
            _roleService = roleService;
            _notificationService = notificationService;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var roles = await _roleService.GetAllRolesAsync();
            RolesGrid.ItemsSource = roles;
        }

        private void RolesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRole = RolesGrid.SelectedItem as Role;
            if (_selectedRole != null)
            {
                EditingRoleTitle.Text = $"Permissions: {_selectedRole.Name}";
                // Filter out any duplicates from the display, prioritizing 'Allow' (true) if they differ
                var uniquePermissions = _selectedRole.Permissions
                    .OrderByDescending(p => p.CanAccess)
                    .GroupBy(p => p.ModuleName)
                    .Select(g => g.First())
                    .OrderBy(p => p.ModuleName)
                    .ToList();

                PermissionsList.ItemsSource = uniquePermissions;
                PermissionEditor.Visibility = Visibility.Visible;
            }
            else
            {
                PermissionEditor.Visibility = Visibility.Collapsed;
            }
        }

        private async void AddRole_Click(object sender, RoutedEventArgs e)
        {
            var name = NewRoleNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            var success = await _roleService.AddRoleAsync(name, "");
            if (success)
            {
                _notificationService.ShowSuccess($"Role '{name}' created.");
                NewRoleNameBox.Clear();
                await LoadDataAsync();
            }
            else
            {
                _notificationService.ShowError("Role already exists or error occurred.");
            }
        }

        private async void SavePermissions_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null) return;

            var permissions = PermissionsList.ItemsSource as List<RolePermission>;
            if (permissions == null) return;

            await _roleService.UpdateRolePermissionsAsync(_selectedRole.Id, permissions);
            _notificationService.ShowSuccess("Permissions updated successfully.");
        }

        private async void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null) return;
            if (_selectedRole.Name == "Admin")
            {
                _notificationService.ShowError("Cannot delete the Admin role.");
                return;
            }

            if (MessageBox.Show($"Delete role '{_selectedRole.Name}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _roleService.DeleteRoleAsync(_selectedRole.Id);
                await LoadDataAsync();
                _selectedRole = null;
                PermissionEditor.Visibility = Visibility.Collapsed;
            }
        }
    }
}
