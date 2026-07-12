using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class UsersView : UserControl
    {
        private const string SelectUserFirstMessage = "Select a user first.";

        private static readonly SolidColorBrush SuccessBg = new(Color.FromRgb(0xD4, 0xED, 0xDA));
        private static readonly SolidColorBrush SuccessFg = new(Color.FromRgb(0x15, 0x57, 0x24));
        private static readonly SolidColorBrush ErrorBg = new(Color.FromRgb(0xF8, 0xD7, 0xDA));
        private static readonly SolidColorBrush ErrorFg = new(Color.FromRgb(0x72, 0x1C, 0x24));

        /// <summary>
        /// Initializes a new instance of the UsersView control and prepares its UI components.
        /// </summary>
        public UsersView(IUserService userService, IRoleService roleService)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the view by loading users into the grid and populating the roles dropdown.
        /// </summary>
        /// <returns>Completes when users have been refreshed and roles have been loaded.</returns>
        public async Task InitializeAsync()
        {
            await RefreshAsync();
            await LoadRolesAsync();
        }

        /// <summary>
        /// Loads all roles from the database and populates the NewRoleCombo dropdown.
        /// </summary>
        /// <remarks>
        /// The method sets the combo's ItemsSource to the retrieved role list, configures
        /// DisplayMemberPath to "Name" and SelectedValuePath to "Id", and selects the first
        /// role if any exist.
        /// </remarks>
        private async Task LoadRolesAsync() // NOSONAR
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

        /// <summary>
        /// Reloads the user list from the database, assigns a 1-based sequence number to each user's <c>SNo</c>, and updates <c>UsersGrid.ItemsSource</c>.
        /// </summary>
        public async Task RefreshAsync() // NOSONAR
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

        private User? SelectedUser => UsersGrid.SelectedItem as User; // NOSONAR

        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Intentionally empty: selection state is read on-demand via the SelectedUser
            // property rather than reacted to here; the handler only exists to keep the
            // XAML-bound SelectionChanged event wired for future use.
        }

        /// <summary>
        /// Enables the currently selected user account and refreshes the displayed user list.
        /// </summary>
        /// <param name="sender">The control that raised the event.</param>
        /// <param name="e">Event data.</param>
        private async void Enable_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is not User u) { ShowFeedback(SelectUserFirstMessage, false); return; }
            using (var scope = App.CreateDbScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                await userService.ToggleActiveAsync(u.Id, true);
            }
            await RefreshAsync();
            ShowFeedback($"✅ {u.Username} has been enabled.", true);
        }

        /// <summary>
        /// Disables the currently selected user account in the grid.
        /// </summary>
        /// <remarks>
        /// If no user is selected, displays "Select a user first." If the selected user is the current session user, displays "You cannot disable your own account." Otherwise sets the user's active state to false, refreshes the user list, and displays a success message "🚫 {username} has been disabled.".
        /// </remarks>
        private async void Disable_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is not User u) { ShowFeedback(SelectUserFirstMessage, false); return; }
            if (u.Id == AppSession.CurrentUser?.Id) { ShowFeedback("You cannot disable your own account.", false); return; }
            using (var scope = App.CreateDbScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                await userService.ToggleActiveAsync(u.Id, false);
            }
            await RefreshAsync();
            ShowFeedback($"🚫 {u.Username} has been disabled.", true);
        }

        /// <summary>
        /// Opens a password-reset dialog for the currently selected user and, if confirmed, resets that user's password and displays success or error feedback.
        /// </summary>
        /// <param name="sender">The source of the click event.</param>
        /// <param name="e">The routed event data.</param>
        private async void ResetPwd_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is not User u) { ShowFeedback(SelectUserFirstMessage, false); return; }

            var dialog = new PasswordResetDialog(u.Username) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog().GetValueOrDefault())
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

        /// <summary>
        /// Opens a dialog to set/clear the currently selected user's email address (used for password-reset codes).
        /// </summary>
        private async void SetEmail_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is not User u) { ShowFeedback(SelectUserFirstMessage, false); return; }

            var dialog = new SetEmailDialog(u.Username, u.Email) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                using (var scope = App.CreateDbScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    await userService.SetEmailAsync(u.Id, dialog.Email);
                }
                await RefreshAsync();
                ShowFeedback($"✉ Email updated for {u.Username}.", true);
            }
        }

        /// <summary>
        /// Deletes the currently selected user after asking for confirmation and refreshes the displayed user list.
        /// </summary>
        /// <remarks>
        /// If no user is selected, displays an error message. Presents a confirmation dialog before deletion;
        /// on confirmation the user is removed and a success message is shown. Any exception raised during deletion
        /// is displayed as error feedback.
        /// </remarks>
        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser is not User u) { ShowFeedback(SelectUserFirstMessage, false); return; }

            var result = await App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessageAsync(
                $"Permanently delete user '{u.Username}'?\nThis cannot be undone.",
                "Confirm Delete", HotelPOS.Application.Interfaces.DialogButton.YesNo, HotelPOS.Application.Interfaces.DialogIcon.Warning);
            if (result != HotelPOS.Application.Interfaces.DialogResult.Yes) return;

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

        /// <summary>
        /// Creates a new user from the Add User form and updates the displayed users list.
        /// </summary>
        /// <remarks>
        /// Reads the username, password, and selected role from the form controls. If no role is selected, displays an error feedback message and aborts. On success, clears the input fields, refreshes the users grid, and displays a success feedback message; on failure, displays the returned error message.
        /// </remarks>
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

        private void ShowFeedback(string message, bool success) // NOSONAR
        {
            FeedbackText.Text = message;
            FeedbackBorder.Background = success ? SuccessBg : ErrorBg;
            FeedbackText.Foreground = success ? SuccessFg : ErrorFg;
            FeedbackBorder.Visibility = Visibility.Visible;
        }
    }
}
