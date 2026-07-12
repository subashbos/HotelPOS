using HotelPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class BillingView : UserControl
    {
        private void GridTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is CartRow row && _viewModel.UpdateRowCommand.CanExecute(row))
            {
                _viewModel.UpdateRowCommand.Execute(row);
            }
        }

        private void GridTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
        }

        private void QtyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                // Move focus back to search to allow adding more items
                FocusSearch();
            }
        }

        private void PriceTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                FocusSearch();
            }
        }

        private void DiscountBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CheckoutButton.Focus();
                    Keyboard.Focus(CheckoutButton);
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void CartGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Cell editing for QTY/PRICE could be handled by VM if we bind to a collection of items with logic
            // For now, let's just refresh the cart in VM after edit
            Dispatcher.BeginInvoke(new Action(() => _viewModel.AddToCartCommand.Execute(null)), System.Windows.Threading.DispatcherPriority.Background);
        }

        // Helper methods for focus management
        private static DataGridCell? GetCell(DataGrid grid, object item, int column)
        {
            var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
            if (row == null)
            {
                grid.ScrollIntoView(item);
                grid.UpdateLayout();
                row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
            }

            if (row != null)
            {
                var presenter = FindVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(row);
                if (presenter != null)
                {
                    return presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                }
            }
            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                var childOfChild = FindVisualChild<T>(child!);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

    }
}
