using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HotelPOS.Views
{
    public partial class ExpenseEntryDialog : Window
    {
        private readonly ExpenseEntryViewModel _viewModel;

        public ExpenseEntryDialog(ExpenseEntryViewModel viewModel)
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
            KeyDown += ExpenseEntryDialog_KeyDown;

            // Auto-focus on first input field
            Loaded += (s, e) => TitleInput.Focus();
        }

        private void ExpenseEntryDialog_KeyDown(object sender, KeyEventArgs e)
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

        private void Cancel_Click(object sender, RoutedEventArgs e) // NOSONAR - S2325: sets instance DialogResult
        {
            DialogResult = false;
            Close();
        }

        // The window has no native title bar (WindowStyle="None"), so dragging is
        // wired up from the custom header instead.
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) // NOSONAR - S2325: calls instance DragMove
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Input_GotFocus(object sender, RoutedEventArgs e) // NOSONAR
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }
    }
}
