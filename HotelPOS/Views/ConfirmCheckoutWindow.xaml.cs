using HotelPOS.ViewModels;
using System.Windows;

namespace HotelPOS.Views
{
    public partial class ConfirmCheckoutWindow : Window
    {
        public ConfirmCheckoutWindow(ConfirmCheckoutViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
