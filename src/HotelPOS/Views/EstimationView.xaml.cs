using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class EstimationView : UserControl
    {
        private readonly EstimationViewModel _viewModel;

        public EstimationView(EstimationViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            PreviewKeyDown += EstimationView_PreviewKeyDown;
        }

        private void EstimationView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 1. Ctrl + S to Save Estimation
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
            {
                e.Handled = true;
                if (_viewModel.SaveEstimationCommand.CanExecute(null))
                {
                    _viewModel.SaveEstimationCommand.Execute(null);
                }
            }
            // 2. F4 to focus the Estimation Number field
            else if (e.Key == Key.F4)
            {
                e.Handled = true;
                EstimationNumberTextBox.Focus();
            }
            // 3. Enter key behavior inside standard TextBox to move focus forward
            else if (e.Key == Key.Enter)
            {
                var element = Keyboard.FocusedElement as UIElement;
                // Do not interfere with multi-line note inputs or data grids
                if (element is TextBox tb && !tb.AcceptsReturn && !IsDescendantOfDataGrid(tb))
                {
                    e.Handled = true;
                    element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
        }

        private static bool IsDescendantOfDataGrid(DependencyObject obj)
        {
            var parent = VisualTreeHelper.GetParent(obj);
            while (parent != null)
            {
                if (parent is DataGrid)
                {
                    return true;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return false;
        }
    }
}
