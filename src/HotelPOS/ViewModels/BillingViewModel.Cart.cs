using HotelPOS.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Domain.Entities;

namespace HotelPOS.ViewModels
{
    public partial class BillingViewModel : ObservableObject
    {
        [RelayCommand]
        private void AdjustQuantity(object parameter)
        {
            if (parameter is object[] values && values.Length >= 2 && values[0] is CartRow row && int.TryParse(values[1].ToString(), out int delta))
            {
                var item = _allItems.FirstOrDefault(i => i.Id == row.ItemId);
                if (item == null) return;

                if (delta > 0 && item.TrackInventory)
                {
                    var cartItems = _cartService.GetItems(TableNumber);
                    var inCart = cartItems.FirstOrDefault(x => x.ItemId == item.Id)?.Quantity ?? 0;

                    if (inCart + delta > item.StockQuantity)
                    {
                        StatusMessage = $"⚠ Out of stock: {item.Name} (Avail: {item.StockQuantity})";
                        return;
                    }
                }

                _cartService.AddItem(TableNumber, row.ItemId, delta);
                UpdateCart();
            }
        }

        [RelayCommand]
        private void AddToCart(object parameter)
        {
            Item? item = parameter as Item;
            if (item == null && parameter is CartRow row)
            {
                item = _allItems.FirstOrDefault(i => i.Id == row.ItemId);
            }

            if (item == null) return;

            // Stock Check
            if (item.TrackInventory)
            {
                var cartItems = _cartService.GetItems(TableNumber);
                var inCart = cartItems.FirstOrDefault(x => x.ItemId == item.Id)?.Quantity ?? 0;

                if (inCart + 1 > item.StockQuantity)
                {
                    StatusMessage = $"⚠ Out of stock: {item.Name} (Avail: {item.StockQuantity})";
                    return;
                }
            }

            _cartService.AddItem(TableNumber, item);
            UpdateCart();
        }

        [RelayCommand]
        private void DecreaseQuantity(object parameter)
        {
            if (parameter is CartRow row)
            {
                _cartService.AddItem(TableNumber, row.ItemId, -1);
                UpdateCart();
            }
        }

        [RelayCommand]
        private void RemoveFromCart(CartRow row)
        {
            if (row == null) return;
            _cartService.RemoveItem(TableNumber, row.ItemId);
            UpdateCart();
        }

        [RelayCommand]
        public void UpdateRow(CartRow row)
        {
            if (row == null) return;

            if (row.Quantity < 1)
            {
                row.Quantity = 1;
                StatusMessage = "Quantity cannot be less than 1. Use 'Remove' to delete item.";
            }

            // LOOPHOLE FIX: Stock check on manual edit
            var item = _allItems.FirstOrDefault(i => i.Id == row.ItemId);
            if (item != null && item.TrackInventory && row.Quantity > item.StockQuantity)
            {
                StatusMessage = $"⚠ Stock Limit: {item.Name} (Avail: {item.StockQuantity})";
                row.Quantity = Math.Max(1, item.StockQuantity);
                _notificationService.ShowWarning($"{item.Name} quantity capped to available stock ({item.StockQuantity})");
            }

            if (row.Price < 0)
            {
                row.Price = 0;
                StatusMessage = "Price cannot be negative.";
            }

            _cartService.SetQuantity(TableNumber, row.ItemId, row.Quantity);
            _cartService.UpdatePrice(TableNumber, row.ItemId, row.Price);
            UpdateCart();
        }

        [RelayCommand]
        private async Task ClearCart()
        {
            if (Cart.Count == 0) return;

            // Confirm before wiping the entire bill (skip confirmation if no dialog service is wired up)
            if (_dialogService != null)
            {
                var result = await _dialogService.ShowMessageAsync(
                    "Clear all items from the current bill?",
                    "Clear Cart",
                    DialogButton.YesNo,
                    DialogIcon.Warning);

                if (result != DialogResult.Yes) return;
            }

            _cartService.Clear(TableNumber);
            DiscountAmount = 0;   // LOOPHOLE FIX: reset discount when cart is cleared
            UpdateCart();
            CartCleared?.Invoke();
        }

        private void UpdateCart()
        {
            var items = _cartService.GetItems(TableNumber);
            SyncCartItems(items);

            Subtotal = _cartService.GetSubtotal(TableNumber);
            GstAmount = IsCompositionScheme ? 0 : _cartService.GetGstAmount(TableNumber);
            TotalAmount = Math.Max(0, Subtotal + GstAmount - DiscountAmount);

            if (!IsTableless)
            {
                SyncActiveTabs();
            }
        }

        private void SyncCartItems(List<OrderItem> items)
        {
            var toRemove = Cart.Where(row => !items.Any(i => i.ItemId == row.ItemId)).ToList();
            foreach (var row in toRemove) Cart.Remove(row);

            foreach (var item in items)
            {
                var existing = Cart.FirstOrDefault(r => r.ItemId == item.ItemId);
                if (existing != null)
                {
                    existing.Quantity = item.Quantity;
                    existing.Price = item.Price;
                    existing.TaxPercentage = item.TaxPercentage;
                    existing.TaxAmount = Math.Round(item.Price * item.Quantity * (item.TaxPercentage / 100m), 2);
                    existing.Total = item.Total;
                }
                else
                {
                    Cart.Add(new CartRow
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        TaxPercentage = item.TaxPercentage,
                        TaxAmount = Math.Round(item.Price * item.Quantity * (item.TaxPercentage / 100m), 2),
                        Total = item.Total
                    });
                }
            }

            var sortedList = Cart.OrderBy(r => r.ItemName).ToList();
            for (int i = 0; i < sortedList.Count; i++)
            {
                var row = sortedList[i];
                int oldIndex = Cart.IndexOf(row);
                if (oldIndex != i)
                {
                    Cart.Move(oldIndex, i);
                }
                row.SNo = i + 1;
            }
        }

        private void SyncActiveTabs()
        {
            var currentActive = _cartService.GetActiveTables() ?? new List<int>();
            currentActive = currentActive.Where(t => t > 0).ToList();

            foreach (var t in currentActive.Where(t => !ActiveTabs.Contains(t)))
                ActiveTabs.Add(t);

            var toRemoveTabs = ActiveTabs.Where(t => !currentActive.Contains(t) && t != TableNumber).ToList();
            foreach (var t in toRemoveTabs) ActiveTabs.Remove(t);

            if (Cart.Count > 0 && TableNumber > 0 && !ActiveTabs.Contains(TableNumber))
                ActiveTabs.Add(TableNumber);

            var sorted = ActiveTabs.OrderBy(t => t).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int oldIndex = ActiveTabs.IndexOf(sorted[i]);
                if (oldIndex != i) ActiveTabs.Move(oldIndex, i);
            }
        }

        partial void OnDiscountAmountChanged(decimal value)
        {
            if (value < 0)
            {
                DiscountAmount = 0;
                _notificationService.ShowWarning("Discount cannot be negative.");
            }
            else if (value > Subtotal)
            {
                DiscountAmount = Subtotal;
                _notificationService.ShowWarning("Discount cannot exceed the subtotal.");
            }
            UpdateCart();
        }
    }
}
