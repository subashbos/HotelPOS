using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace HotelPOS.Views
{
    public partial class CustomerHistoryDialog : Window
    {
        public CustomerHistoryDialog(CustomerHistoryViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Close_Click(object sender, RoutedEventArgs e) // NOSONAR
        {
            Close();
        }

        // The window has no native title bar (WindowStyle="None"), so dragging is
        // wired up from the custom header instead.
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) // NOSONAR
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
