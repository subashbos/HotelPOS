using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class UsersView : UserControl
    {
        private static readonly SolidColorBrush SuccessBg = new(Color.FromRgb(0xD4, 0xED, 0xDA));
        private static readonly SolidColorBrush SuccessFg = new(Color.FromRgb(0x15, 0x57, 0x24));
        private static readonly SolidColorBrush ErrorBg = new(Color.FromRgb(0xF8, 0xD7, 0xDA));
        private static readonly SolidColorBrush ErrorFg = new(Color.FromRgb(0x72, 0x1C, 0x24));

        public UsersView(IUserService userService, IRoleService roleService)
        {
            InitializeComponent();
        }

        public async Task InitializeAsync()
        {
            await RefreshAsync();
            await LoadRolesAsync();
        }

        private async Task LoadRolesAsync()
        {
            List<Role> roles;
            using (var scope = App.CreateDbScope())
            {
                var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
                roles = await roleService.GetAllRolesAsync();
            }
            NewRoleCombo.ItemsSource = roles;
            NewRoleCombo.DisplayMemberPath = "Name";
            NewRoleCombo.SelectedValuePath = "Id";
            if (roles.Any()) NewRoleCombo.SelectedIndex = 0;
        }

        public async Task RefreshAsync()
        {
            List<User> users;
            using (var scope = App.CreateDbScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                users = await userService.GetAllUsersAsync();
            }
            for (int i = 0; i < users.Count; i++) users[i].SNo = i + 1;
            UsersGrid.ItemsSource = users;
        }

        private User? SelectedUser => UsersGrid.SelectedItem as User;

        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private async void Enable_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is not User u) { ShowFeedback("Select a user first.", false); return; }
            using (var scope = App.CreateDbScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                await userService.ToggleActiveAsync(u.Id, true);
            }
            await RefreshAsync();
            ShowFeedback($"✅ {u.Username} has been enabled.", true);
        }

        private async void Disable_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is not User u) { ShowFeedback("Select a user first.", false); return; }
            if (u.Id == AppSession.CurrentUser?.Id) { ShowFeedback("You cannot disable your own account.", false); return; }
            using (var scope = App.CreateDbScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                await userService.ToggleActiveAsync(u.Id, false);
            }
            await RefreshAsync();
            ShowFeedback($"🚫 {u.Username} has been disabled.", true);
        }

        private async void ResetPwd_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is not User u) { ShowFeedback("Select a user first.", false); return; }

            var dialog = new PasswordResetDialog(u.Username) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                bool ok;
                string err;
                using (var scope = App.CreateDbScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    (ok, err) = await userService.ResetPasswordAsync(u.Id, dialog.NewPassword);
                }
                ShowFeedback(ok ? $"🔑 Password reset for {u.Username}." : err, ok);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is not User u) { ShowFeedback("Select a user first.", false); return; }

            var result = MessageBox.Show(
                $"Permanently delete user '{u.Username}'?\nThis cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    await userService.DeleteUserAsync(u.Id, AppSession.CurrentUser?.Id ?? 0);
                }
                await RefreshAsync();
                ShowFeedback($"🗑 {u.Username} deleted.", true);
            }
            catch (Exception ex) { ShowFeedback(ex.Message, false); }
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var username = NewUsernameBox.Text.Trim();
            var password = NewPasswordBox.Password;
            var selectedRole = NewRoleCombo.SelectedItem as Role;

            if (selectedRole == null)
            {
                ShowFeedback("Please select a role.", false);
                return;
            }

            bool ok;
            string err;
            using (var scope = App.CreateDbScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                (ok, err) = await userService.AddUserAsync(username, password, selectedRole.Name, selectedRole.Id);
            }

            if (ok)
            {
                NewUsernameBox.Clear();
                NewPasswordBox.Clear();
                await RefreshAsync();
                ShowFeedback($"✅ User '{username}' created successfully.", true);
            }
            else
            {
                ShowFeedback(err, false);
            }
        }

        private void ShowFeedback(string message, bool success)
        {
            FeedbackText.Text = message;
            FeedbackBorder.Background = success ? SuccessBg : ErrorBg;
            FeedbackText.Foreground = success ? SuccessFg : ErrorFg;
            FeedbackBorder.Visibility = Visibility.Visible;
        }
    }
}
