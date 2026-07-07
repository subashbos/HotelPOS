using System.Windows;

namespace HotelPOS.Views
{
    public partial class TwoFactorChallengeDialog : Window
    {
        public string Code => CodeBox.Text.Trim();

        public TwoFactorChallengeDialog(string username)
        {
            InitializeComponent();
            TitleText.Text = $"Verify it's you: {username}";
            CodeBox.Focus();
        }

        private void Verify_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
