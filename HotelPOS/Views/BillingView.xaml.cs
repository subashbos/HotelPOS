using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HotelPOS.ViewModels;
using HotelPOS;

namespace HotelPOS.Views
{
    public partial class BillingView : UserControl
    {
        private readonly BillingViewModel _viewModel;

        public BillingView(BillingViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) =>
            {
                await _viewModel.InitializeAsync();
            };

            // Relay the ViewModel's navigation event outward to the shell
            _viewModel.OrderUpdated += () => OrderUpdated?.Invoke();
        }

        public event Action? OrderUpdated;

        public void LoadOrderForEdit(Order order)
        {
            _viewModel.LoadOrderForEdit(order);
        }

        public void FocusSearch()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SearchBox.Focus();
                Keyboard.Focus(SearchBox);
                SearchBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        public void TriggerCheckout()
        {
            if (_viewModel.SaveOrderCommand.CanExecute(null))
            {
                _viewModel.SaveOrderCommand.Execute(null);
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F4) 
            {
                _viewModel.SaveOrderCommand.Execute(null);
            }
            else if (e.Key == Key.F1 || e.Key == Key.F3 || (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SearchBox.Focus();
                    Keyboard.Focus(SearchBox);
                    SearchBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
            else if (e.Key == Key.Enter)
            {
                // If focus is not on an input that handles Enter (like SearchBox or AutoList)
                // and we are not currently editing a cell in the cart grid, then Enter = Checkout
                if (!SearchBox.IsFocused && !AutoList.IsFocused && !CartGrid.IsKeyboardFocusWithin)
                {
                    if (_viewModel.SaveOrderCommand.CanExecute(null))
                    {
                        _viewModel.SaveOrderCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (AutoPopup.IsOpen && AutoList.SelectedItem is Item selected)
                {
                    AddItemFromAutoComplete(selected);
                    e.Handled = true;
                    return;
                }
                else if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    // Empty search box + Enter = Checkout/Preview
                    if (_viewModel.SaveOrderCommand.CanExecute(null))
                    {
                        _viewModel.SaveOrderCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }

            if (!AutoPopup.IsOpen) return;

            if (e.Key == Key.Down)
            {
                if (AutoList.SelectedIndex < AutoList.Items.Count - 1)
                {
                    AutoList.SelectedIndex++;
                    AutoList.ScrollIntoView(AutoList.SelectedItem);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (AutoList.SelectedIndex > 0)
                {
                    AutoList.SelectedIndex--;
                    AutoList.ScrollIntoView(AutoList.SelectedItem);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                AutoPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void AddItemFromAutoComplete(Item item)
        {
            AutoPopup.IsOpen = false;
            SearchBox.Text = string.Empty;
            _viewModel.AddToCartCommand.Execute(item);
            
            // Focus the quantity field of the added item
            FocusQuantityOfItem(item.Id);
        }

        private void FocusQuantityOfItem(int itemId)
        {
            // Wait for VM to update collection and UI to render
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var rowData = _viewModel.Cart.FirstOrDefault(r => r.ItemId == itemId);
                if (rowData == null) return;

                CartGrid.ScrollIntoView(rowData);
                CartGrid.UpdateLayout();

                // Find the TextBox in the QTY column (Index 2)
                var cell = GetCell(CartGrid, rowData, 2);
                if (cell != null)
                {
                    var textBox = FindVisualChild<TextBox>(cell);
                    if (textBox != null)
                    {
                        textBox.Focus();
                        textBox.SelectAll();
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                AutoPopup.IsOpen = false;
            }
            else if (SearchBox.IsFocused)
            {
                AutoPopup.IsOpen = true;
                if (AutoList.Items.Count > 0) AutoList.SelectedIndex = 0;
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                AutoPopup.IsOpen = true;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
            {
                if (!AutoList.IsKeyboardFocusWithin && !AutoList.IsMouseOver)
                    AutoPopup.IsOpen = false;
            }));
        }

        private void ItemCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Item item)
                _viewModel.AddToCartCommand.Execute(item);
        }
        private void CartGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Cell editing for QTY/PRICE could be handled by VM if we bind to a collection of items with logic
            // For now, let's just refresh the cart in VM after edit
            Dispatcher.BeginInvoke(new Action(() => _viewModel.AddToCartCommand.Execute(null)), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (AutoPopup.IsOpen && AutoList.Items.Count > 0)
                AddItemFromAutoComplete(AutoList.SelectedItem as Item ?? (Item)AutoList.Items[0]);
        }

        private void AutoList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && AutoList.SelectedItem is Item item)
            {
                AddItemFromAutoComplete(item);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                AutoPopup.IsOpen = false;
                SearchBox.Focus();
                e.Handled = true;
            }
        }

        private void AutoList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;
            var element = e.OriginalSource as DependencyObject;
            while (element != null && element is not ListBoxItem)
                element = VisualTreeHelper.GetParent(element);

            if (element is ListBoxItem lbi && lbi.DataContext is Item item)
            {
                e.Handled = true;
                AddItemFromAutoComplete(item);
            }
        }
        private void GridTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is CartRow row)
            {
                if (_viewModel.UpdateRowCommand.CanExecute(row))
                {
                    _viewModel.UpdateRowCommand.Execute(row);
                }
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
                // Move focus back to search
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SearchBox.Focus();
                    SearchBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void PriceTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SearchBox.Focus();
                    SearchBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        // Helper methods for focus management
        private DataGridCell? GetCell(DataGrid grid, object item, int column)
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

        private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t) return t;
                var childOfChild = FindVisualChild<T>(child!);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }
    }
}
