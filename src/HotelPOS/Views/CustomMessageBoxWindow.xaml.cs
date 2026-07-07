using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace HotelPOS.Views
{
    public partial class CustomMessageBoxWindow : Window
    {
        public CustomMessageBoxWindow(CustomMessageBoxViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.CloseAction = () =>
            {
                DialogResult = true;
                Close();
            };
        }

        // Allow dragging the window from anywhere
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
