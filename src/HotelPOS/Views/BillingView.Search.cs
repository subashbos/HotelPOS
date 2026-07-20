using HotelPOS.Domain.Entities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class BillingView : UserControl
    {
        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && HandleSearchEnterKey())
            {
                e.Handled = true;
                return;
            }

            if (!AutoPopup.IsOpen) return;

            HandleAutoListNavigationKey(e);
        }

        private bool HandleSearchEnterKey() // NOSONAR
        {
            if (AutoPopup.IsOpen && AutoList.Items.Count > 0)
            {
                var selected = AutoList.SelectedItem as Item ?? AutoList.Items[0] as Item;
                if (selected != null)
                {
                    AddItemFromAutoComplete(selected);
                    return true;
                }
            }

            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                // Empty search box + Enter = move to payment mode
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PaymentModeCombo.Focus();
                    PaymentModeCombo.IsDropDownOpen = true;
                }), System.Windows.Threading.DispatcherPriority.Input);
                return true;
            }

            return false;
        }

        private void HandleAutoListNavigationKey(KeyEventArgs e) // NOSONAR
        {
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

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) // NOSONAR
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

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e) // NOSONAR
        {
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                AutoPopup.IsOpen = true;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e) // NOSONAR
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
            {
                _viewModel.AddToCartCommand.Execute(item);
                FocusQuantityOfItem(item.Id);
            }
        }

        private void AddItem_Click(object sender, RoutedEventArgs e) // NOSONAR
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
    }
}
