using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HotelPOS.Views
{
    public partial class TwoFactorEnrollDialog : Window
    {
        public string Secret { get; }

        public TwoFactorEnrollDialog(string username)
        {
            InitializeComponent();
            Secret = TotpGenerator.GenerateSecret();
            SecretBox.Text = Secret;
            UriBox.Text = TotpGenerator.BuildOtpAuthUri(Secret, username);
            CodeBox.Focus();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (!TotpGenerator.ValidateCode(Secret, CodeBox.Text))
            {
                var ns = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<INotificationService>();
                ns.ShowError("That code didn't match. Check the time on your device and try again.");
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
