using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class PurchaseEntryView : UserControl
    {
        private readonly PurchaseEntryViewModel _viewModel;

        public PurchaseEntryView(PurchaseEntryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            PreviewKeyDown += PurchaseEntryView_PreviewKeyDown;
        }

        private void PurchaseEntryView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 1. Ctrl + S to Save Purchase
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
            {
                e.Handled = true;
                if (_viewModel.SavePurchaseCommand.CanExecute(null))
                {
                    _viewModel.SavePurchaseCommand.Execute(null);
                }
            }
            // 2. F4 to Focus Supplier Dropdown
            else if (e.Key == Key.F4)
            {
                e.Handled = true;
                SupplierComboBox.Focus();
            }
            // 3. Enter key behavior inside standard TextBox to move focus forward
            else if (e.Key == Key.Enter)
            {
                var element = Keyboard.FocusedElement as UIElement;
                if (element is TextBox tb)
                {
                    // Do not interfere with multi-line note inputs or data grids
                    if (tb.AcceptsReturn == false && !IsDescendantOfDataGrid(tb))
                    {
                        e.Handled = true;
                        element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
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
