using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HotelPOS.Views
{
    public partial class EmployeeEntryDialog : Window
    {
        private readonly EmployeeEntryViewModel _viewModel;

        public EmployeeEntryDialog(EmployeeEntryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.RequestClose += (s, success) =>
            {
                DialogResult = success;
                Close();
            };

            KeyDown += EmployeeEntryDialog_KeyDown;
            Loaded += (s, e) => FirstNameInput.Focus();
        }

        private void EmployeeEntryDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (FocusManager.GetFocusedElement(this) is TextBox tb && tb.AcceptsReturn)
                {
                    return;
                }

                e.Handled = true;
                var request = new TraversalRequest(FocusNavigationDirection.Next);
                if (Keyboard.FocusedElement is UIElement element)
                {
                    element.MoveFocus(request);
                }
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                if (_viewModel.SaveCommand.CanExecute(null))
                {
                    _viewModel.SaveCommand.Execute(null);
                }
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) // NOSONAR
        {
            DialogResult = false;
            Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) // NOSONAR
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
