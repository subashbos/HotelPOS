using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
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

        /// <summary>Only true for the window shown at process cold-start; see App.ShowLoginWindow.</summary>
        public bool AllowAutoLogin { get; set; }

        // DI resolves this constructor via the login scope created in App.ShowLoginWindow()
        public LoginWindow(IAuthService authService, IUserService userService, INotificationService notificationService)
        {
            InitializeComponent();
            _authService = authService;
            _userService = userService;
            _notificationService = notificationService;
            Loaded += async (s, e) =>
            {
                UsernameBox.Focus();
                if (AllowAutoLogin)
                {
                    await TryAutoLoginAsync();
                }
            };
        }

        /// <summary>Attempts a silent "remember me" login using the locally saved token, if any.</summary>
        private async Task TryAutoLoginAsync()
        {
            var saved = Services.RememberMeStore.Load();
            if (saved == null) return;

            var (username, token) = saved.Value;

            try
            {
                User? user;
                string? newToken = null;
                using (var scope = App.CreateDbScope())
                {
                    var rememberMeService = scope.ServiceProvider.GetRequiredService<IRememberMeService>();
                    user = await rememberMeService.ValidateAndConsumeAsync(username, token);
                    if (user != null)
                    {
                        newToken = await rememberMeService.IssueTokenAsync(user.Id);
                    }
                }

                if (user == null)
                {
                    Services.RememberMeStore.Clear();
                    return;
                }

                Services.RememberMeStore.Save(user.Username, newToken!);
                AppSession.CurrentUser = user;
                CompleteLogin();
            }
            catch
            {
                // Best-effort: if the DB isn't ready yet or anything goes wrong, just fall back to manual login.
            }
        }

        /// <summary>
        /// Handles the Log In button click: validates credentials, authenticates the user, enforces a required password reset when applicable, and transitions to the dashboard session.
        /// </summary>
        /// <remarks>
        /// Shows inline error messages for validation or authentication failures and uses short-lived DI scopes for authentication and password-reset operations. If authentication succeeds and no password change is required, sets AppSession.CurrentUser, creates a dashboard scope for the session, shows the dashboard as the application's main window, and disposes the session scope when the dashboard closes. Exceptions are caught and displayed in the UI.
        /// </remarks>
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

                HotelPOS.Domain.Entities.User? user = null;
                using (var scope = App.CreateDbScope())
                {
                    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                    user = await authService.AuthenticateAsync(username, password);
                }

                if (user == null)
                {
                    ErrorText.Text = "Invalid username or password.";
                    ErrorText.Visibility = Visibility.Visible;
                }
                else
                {
                    if (user.TwoFactorEnabled)
                    {
                        var challenge = new Views.TwoFactorChallengeDialog(user.Username) { Owner = this };
                        if (challenge.ShowDialog() != true) return; // user cancelled

                        if (!HotelPOS.Domain.Common.TotpGenerator.ValidateCode(user.TwoFactorSecret, challenge.Code))
                        {
                            ErrorText.Text = "Invalid authentication code.";
                            ErrorText.Visibility = Visibility.Visible;
                            return;
                        }
                    }

                    if (user.MustChangePassword)
                    {
                        // ResetPasswordAsync authorizes "resetting your own account" via AppSession —
                        // but AppSession isn't set until after this block. Establish it temporarily
                        // (the primary credentials were already verified above) so the self-service
                        // check passes, then clear it again on every exit path below since the user
                        // hasn't actually completed login yet.
                        AppSession.CurrentUser = user;

                        var dialog = new Views.PasswordResetDialog(user.Username) { Owner = this };
                        if (dialog.ShowDialog() == true)
                        {
                            bool ok = false;
                            string? err = null;
                            using (var pwScope = App.CreateDbScope())
                            {
                                var userService = pwScope.ServiceProvider.GetRequiredService<IUserService>();
                                var res = await userService.ResetPasswordAsync(user.Id, dialog.NewPassword);
                                ok = res.Success;
                                err = res.Error;
                            }

                            AppSession.CurrentUser = null;

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
                            AppSession.CurrentUser = null;
                            return; // User cancelled password change, don't log in
                        }
                    }

                    AppSession.CurrentUser = user;

                    // Any previously saved remember-me credential belongs to whichever account checked
                    // the box last; clear it up front so an unchecked login never leaves a stale token behind.
                    Services.RememberMeStore.Clear();
                    if (RememberMeCheck.IsChecked == true)
                    {
                        using var rmScope = App.CreateDbScope();
                        var rememberMeService = rmScope.ServiceProvider.GetRequiredService<IRememberMeService>();
                        var token = await rememberMeService.IssueTokenAsync(user.Id);
                        Services.RememberMeStore.Save(user.Username, token);
                    }

                    CompleteLogin();
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

        /// <summary>Transitions from a verified AppSession.CurrentUser to the dashboard session.</summary>
        private void CompleteLogin()
        {
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
                // Show a fresh login screen for the next session (never auto-login post-logout — see App.ShowLoginWindow)
                app.ShowLoginWindow();
            };

            dashboard.Show();
            System.Windows.Application.Current.MainWindow = dashboard;
            Close();
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Login_Click(sender, e);
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            var forgotDialog = new Views.ForgotPasswordDialog { Owner = this };
            if (forgotDialog.ShowDialog() != true) return;

            var codeDialog = new Views.ResetWithCodeDialog(forgotDialog.Username) { Owner = this };
            codeDialog.ShowDialog();
        }
    }
}
