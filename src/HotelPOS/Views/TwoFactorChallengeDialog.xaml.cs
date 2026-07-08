using System.Windows;

namespace HotelPOS.Views
{
    public partial class TwoFactorChallengeDialog : Window
    {
        public string Code => CodeBox.Text.Trim(); // NOSONAR - reads this dialog instance's own CodeBox control; cannot be static

        public TwoFactorChallengeDialog(string username)
        {
            InitializeComponent();
            TitleText.Text = $"Verify it's you: {username}";
            CodeBox.Focus();
        }

        private void Verify_Click(object sender, RoutedEventArgs e) => DialogResult = true; // NOSONAR - sets this dialog instance's inherited Window.DialogResult; cannot be static

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false; // NOSONAR - sets this dialog instance's inherited Window.DialogResult; cannot be static
    }
}
