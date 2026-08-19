using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HotelPOS.Views.Common
{
    /// <summary>
    /// Base class for the borderless "create/edit" entry dialogs (customer, supplier,
    /// expense, employee, ...). Centralizes the keyboard navigation, custom-titlebar
    /// drag, cancel, and focus-select behavior that was previously copy-pasted into
    /// each dialog's code-behind.
    /// </summary>
    public abstract class EntryDialogWindow : Window
    {
        private IEntryDialogViewModel? _viewModel;

        protected void InitializeEntryDialog(IEntryDialogViewModel viewModel, Control? initialFocus = null)
        {
            _viewModel = viewModel;
            DataContext = viewModel;

            viewModel.RequestClose += (s, success) =>
            {
                DialogResult = success;
                Close();
            };

            KeyDown += EntryDialog_KeyDown;

            if (initialFocus != null)
            {
                Loaded += (s, e) => initialFocus.Focus();
            }
        }

        private void EntryDialog_KeyDown(object sender, KeyEventArgs e)
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
                if (_viewModel?.SaveCommand.CanExecute(null) == true)
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

        protected void Cancel_Click(object sender, RoutedEventArgs e) // NOSONAR - S2325: sets instance DialogResult
        {
            DialogResult = false;
            Close();
        }

        // The window has no native title bar (WindowStyle="None"), so dragging is
        // wired up from the custom header instead.
        protected void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) // NOSONAR - S2325: calls instance DragMove
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        protected void Input_GotFocus(object sender, RoutedEventArgs e) // NOSONAR
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }
    }
}
