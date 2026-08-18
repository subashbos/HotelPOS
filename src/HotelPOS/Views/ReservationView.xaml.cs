using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HotelPOS.Views
{
    public partial class ReservationView : UserControl
    {
        private readonly ReservationViewModel _viewModel;

        public ReservationView(ReservationViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            PreviewKeyDown += ReservationView_PreviewKeyDown;
        }

        private void ReservationView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 1. Ctrl + S to Book Table
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
            {
                e.Handled = true;
                if (_viewModel.SaveReservationCommand.CanExecute(null))
                {
                    _viewModel.SaveReservationCommand.Execute(null);
                }
            }
            // 2. F4 to focus the Start Time field
            else if (e.Key == Key.F4)
            {
                e.Handled = true;
                StartTimeTextBox.Focus();
            }
            // 3. Enter key behavior inside standard TextBox to move focus forward
            else if (e.Key == Key.Enter)
            {
                var element = Keyboard.FocusedElement as UIElement;
                if (element is TextBox tb && !tb.AcceptsReturn)
                {
                    e.Handled = true;
                    element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
        }
    }
}
