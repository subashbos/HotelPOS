using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace HotelPOS
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private IServiceScope? _sessionScope;

        // DI resolves this constructor via the login scope created in App.ShowLoginWindow()
        public LoginWindow(IAuthService authService, IUserService userService, INotificationService notificationService)
        {
            InitializeComponent();
            _authService = authService;
            _userService = userService;
            _notificationService = notificationService;
            Loaded += (s, e) => UsernameBox.Focus();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Please enter both fields.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                LoginButton.IsEnabled = false;
                LoginButton.Content = "Authenticating...";

                var user = await _authService.AuthenticateAsync(username, password);

                if (user == null)
                {
                    ErrorText.Text = "Invalid username or password.";
                    ErrorText.Visibility = Visibility.Visible;
                }
                else
                {
                    if (user.MustChangePassword)
                    {
                        var dialog = new Views.PasswordResetDialog(user.Username) { Owner = this };
                        if (dialog.ShowDialog() == true)
                        {
                            var (ok, err) = await _userService.ResetPasswordAsync(user.Id, dialog.NewPassword);
                            if (!ok)
                            {
                                _notificationService.ShowError($"Failed to update password: {err}");
                                return;
                            }
                            _notificationService.ShowSuccess("Password updated successfully. You can now log in with your new password.");
                            PasswordBox.Clear();
                            return;
                        }
                        else
                        {
                            return; // User cancelled password change, don't log in
                        }
                    }

                    AppSession.CurrentUser = user;

                    var app = (App)System.Windows.Application.Current;
                    var (scope, dashboard) = app.CreateDashboardScope();
                    _sessionScope = scope;

                    // Clear Tag so App.ShowLoginWindow() doesn't double-dispose the login scope
                    Tag = null;

                    dashboard.Closed += (_, __) =>
                    {
                        // Dispose all Scoped services (repositories, DbContext) for this session
                        _sessionScope?.Dispose();
                        _sessionScope = null;
                        // Show a fresh login screen for the next session
                        app.ShowLoginWindow();
                    };

                    dashboard.Show();
                    System.Windows.Application.Current.MainWindow = dashboard;
                    Close();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Login failure for user {Username}", username);
                ErrorText.Text = "Login failed: " + ex.Message;
                ErrorText.Visibility = Visibility.Visible;
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Log In";
            }
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Login_Click(sender, e);
        }
    }
}
