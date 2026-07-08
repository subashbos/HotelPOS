using HotelPOS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HotelPOS.Views
{
    public partial class ForgotPasswordDialog : Window
    {
        public string Username { get; private set; } = string.Empty;

        public ForgotPasswordDialog()
        {
            InitializeComponent();
            UsernameBox.Focus();
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                var ns = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<INotificationService>();
                ns.ShowError("Enter a username.");
                return;
            }

            try
            {
                SendButton.IsEnabled = false;
                SendButton.Content = "Sending...";

                using (var scope = App.CreateDbScope())
                {
                    var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();
                    await resetService.RequestResetAsync(username);
                }

                Username = username;
                var notify = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<INotificationService>();
                notify.ShowSuccess("If that account has an email on file, a reset code has been sent to it.");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                var ns = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<INotificationService>();
                ns.ShowError($"Could not send reset code: {ex.Message}");
            }
            finally
            {
                SendButton.IsEnabled = true;
                SendButton.Content = "Send Reset Code";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false; // NOSONAR - sets this dialog instance's inherited Window.DialogResult; cannot be static
    }
}
