using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class PrinterSettingsView : UserControl
    {
        public PrinterSettingsView()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Settings saved successfully (Simulated).";
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
