using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HotelPOS.Views
{
    public partial class BillingView : UserControl
    {
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e) // NOSONAR
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(250));
        }

        private void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e) // NOSONAR
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string proposedText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(proposedText, @"^\d*\.?\d*$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(250));
        }

        private void DataObject_OnPasting(object sender, DataObjectPastingEventArgs e) // NOSONAR
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!System.Text.RegularExpressions.Regex.IsMatch(text, "^[0-9]+$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(250))) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private void Decimal_OnPasting(object sender, DataObjectPastingEventArgs e) // NOSONAR
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d*\.?\d*$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(250))) e.CancelCommand();
            }
            else e.CancelCommand();
        }
    }
}
