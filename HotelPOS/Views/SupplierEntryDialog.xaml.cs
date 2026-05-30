using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HotelPOS.Views
{
    public partial class SupplierEntryDialog : Window
    {
        private readonly SupplierEntryViewModel _viewModel;

        public SupplierEntryDialog(SupplierEntryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.RequestClose += (s, success) =>
            {
                DialogResult = success;
                Close();
            };

            // Keyboard navigation setup
            KeyDown += SupplierEntryDialog_KeyDown;

            // Auto-focus on first input field
            Loaded += (s, e) => NameInput.Focus();
        }

        private void SupplierEntryDialog_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter -> Move focus to next field
            if (e.Key == Key.Enter)
            {
                // Prevent multi-line TextBox from losing Enter capability
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
            // Ctrl + S -> Save
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                if (_viewModel.SaveCommand.CanExecute(null))
                {
                    _viewModel.SaveCommand.Execute(null);
                }
            }
            // Escape -> Cancel
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Input_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }
    }
}
