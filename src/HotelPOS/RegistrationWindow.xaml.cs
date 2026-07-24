#nullable enable

using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using System.Windows;
using System.Windows.Input;

namespace HotelPOS
{
    public partial class RegistrationWindow : Window
    {
        private readonly INotificationService _notificationService;

        // DI resolves this constructor via the scope created in App.ShowRegistrationWindow
        public RegistrationWindow(INotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;
            UsernameBox.Text = "admin";
            Loaded += (s, e) => PasswordBox.Focus();
        }

        private async void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Please enter a username.");
                return;
            }

            if (password.Length < ValidationLimits.MinPasswordLength)
            {
                ShowError($"Password must be at least {ValidationLimits.MinPasswordLength} characters.");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Passwords do not match.");
                return;
            }

            try
            {
                CreateButton.IsEnabled = false;
                CreateButton.Content = "Creating...";

                var (success, error) = await App.CurrentApp!.CreateInitialAdminAsync(username, password);
                if (!success)
                {
                    ShowError(error);
                    return;
                }

                // Show the login window (and let it become the new MainWindow) before firing the
                // success toast, and only close this window afterward - GetNotifier() anchors to
                // Application.Current.MainWindow at call time, and closing the anchor window while
                // a toast is still alive/animating throws "Cannot set Visibility ... after a Window
                // has closed" from inside the ToastNotifications library.
                App.CurrentApp.ShowLoginWindow(allowAutoLogin: false);
                _notificationService.ShowSuccess("Administrator account created. You can now log in.");
                Close();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to create initial admin account for {Username}", username);
                ShowError("Failed to create account: " + ex.Message);
            }
            finally
            {
                CreateButton.IsEnabled = true;
                CreateButton.Content = "Create Account";
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void Input_KeyDown(object sender, KeyEventArgs e) // NOSONAR
        {
            if (e.Key == Key.Enter)
                CreateAccount_Click(sender, e);
        }
    }
}
