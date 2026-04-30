using System.Windows;

namespace HotelPOS.Views
{
    public partial class PasswordResetDialog : Window
    {
        private const int MinimumPasswordLength = 10;
        public string NewPassword { get; private set; } = string.Empty;

        public PasswordResetDialog(string username)
        {
            InitializeComponent();
            TitleText.Text = $"Reset password for: {username}";
            PwdBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (PwdBox.Password.Length < MinimumPasswordLength)
            {
                MessageBox.Show($"Password must be at least {MinimumPasswordLength} characters.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NewPassword = PwdBox.Password;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
