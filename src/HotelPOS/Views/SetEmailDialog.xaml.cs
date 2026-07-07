using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HotelPOS.Views
{
    public partial class SetEmailDialog : Window
    {
        public string? Email { get; private set; }

        public SetEmailDialog(string username, string? currentEmail)
        {
            InitializeComponent();
            TitleText.Text = $"Set email for: {username}";
            EmailBox.Text = currentEmail ?? string.Empty;
            EmailBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailBox.Text.Trim();
            if (!string.IsNullOrEmpty(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                var ns = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.INotificationService>();
                ns.ShowError("Enter a valid email address, or leave it blank to clear it.");
                return;
            }

            Email = string.IsNullOrEmpty(email) ? null : email;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
