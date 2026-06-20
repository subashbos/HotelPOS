using HotelPOS.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HotelPOS.Views
{
    public partial class ConfirmCheckoutWindow : Window
    {
        public ConfirmCheckoutWindow(ConfirmCheckoutViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var fullText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength).Insert(textBox.SelectionStart, e.Text);
                Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]{0,2})?$", RegexOptions.None, TimeSpan.FromMilliseconds(250));
                e.Handled = !regex.IsMatch(fullText);
            }
        }

        private void NumericOnly_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }
    }
}
