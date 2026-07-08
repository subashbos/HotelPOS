using HotelPOS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HotelPOS.Views
{
    public partial class ResetWithCodeDialog : Window
    {
        private readonly string _username;

        public ResetWithCodeDialog(string username)
        {
            InitializeComponent();
            _username = username;
            TitleText.Text = $"Reset password for: {username}";
            CodeBox.Focus();
        }

        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var notify = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<INotificationService>();
            var code = CodeBox.Text.Trim();
            var newPassword = PwdBox.Password;

            if (string.IsNullOrEmpty(code))
            {
                notify.ShowError("Enter the code from your email.");
                return;
            }

            try
            {
                ConfirmButton.IsEnabled = false;
                ConfirmButton.Content = "Resetting...";

                bool success;
                string? error;
                using (var scope = App.CreateDbScope())
                {
                    var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();
                    (success, error) = await resetService.ConfirmResetAsync(_username, code, newPassword);
                }

                if (!success)
                {
                    notify.ShowError(error ?? "Could not reset the password.");
                    return;
                }

                notify.ShowSuccess("Password reset successfully. You can now log in with your new password.");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                notify.ShowError($"Could not reset password: {ex.Message}");
            }
            finally
            {
                ConfirmButton.IsEnabled = true;
                ConfirmButton.Content = "Reset Password";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false; // NOSONAR - sets this dialog instance's inherited Window.DialogResult; cannot be static
    }
}
