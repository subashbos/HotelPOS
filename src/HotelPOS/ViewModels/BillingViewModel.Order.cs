using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS.ViewModels
{
    public partial class BillingViewModel : ObservableObject
    {
        private Order? _editingOrder;

        private void LoadHeldOrders()
        {
            HeldOrders.Clear();
            foreach (var h in _cartService.GetHeldOrders())
                HeldOrders.Add(h);
        }

        [RelayCommand]
        private async Task HoldOrder()
        {
            if (Cart.Count == 0) return;

            // LOOPHOLE FIX: Confirm before moving to hold/kitchen (skip confirmation if no dialog service is wired up)
            if (_dialogService != null)
            {
                var result = await _dialogService.ShowMessageAsync(
                    "Send this order to Kitchen / Hold?",
                    "Hold Order",
                    DialogButton.YesNo,
                    DialogIcon.Question);

                if (result != DialogResult.Yes) return;
            }

            _cartService.HoldOrder(TableNumber, $"Table {TableNumber} - {DateTime.Now:hh:mm tt}");

            // Get the newly held order to print KOT
            var newlyHeld = _cartService.GetHeldOrders().OrderByDescending(h => h.HeldAt).FirstOrDefault();

            LoadHeldOrders();
            UpdateCart();
            _notificationService.ShowSuccess("Order moved to Hold / Sent to Kitchen");

            if (newlyHeld != null)
            {
                await PrintKOTAsync(newlyHeld.TableNumber, newlyHeld.Items);
            }
        }

        [RelayCommand]
        private async Task PrintKOTOnly()
        {
            if (Cart.Count == 0) return;
            var items = _cartService.GetItems(TableNumber);
            await PrintKOTAsync(TableNumber, items);
            StatusMessage = "KOT Sent to Kitchen";
        }

        private async Task PrintKOTAsync(int tableNumber, List<OrderItem> items)
        {
            try
            {
                var settings = await _settingService.GetSettingsAsync();

                // Execute on UI thread for printing
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var doc = ReceiptGenerator.CreateKOT(tableNumber, items, settings.ReceiptFormat == "Thermal");

                        var dialog = new System.Windows.Controls.PrintDialog();
                        if (!string.IsNullOrEmpty(settings.DefaultPrinter))
                        {
                            try
                            {
                                dialog.PrintQueue = new System.Printing.LocalPrintServer().GetPrintQueue(settings.DefaultPrinter);
                            }
                            catch { /* Fallback to default if printer not found */ }
                        }

                        dialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, "KOT " + tableNumber);
                    });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"KOT Print failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ToggleHeldOrders()
        {
            LoadHeldOrders();
            IsHeldOrdersPopupOpen = !IsHeldOrdersPopupOpen;
        }

        [RelayCommand]
        private void ResumeOrder(HeldOrder held)
        {
            if (held == null) return;
            _cartService.ResumeHeldOrder(held.Id, TableNumber);
            IsHeldOrdersPopupOpen = false;
            LoadHeldOrders();
            UpdateCart();
            _notificationService.ShowInfo($"Resumed {held.HoldName}");
        }

        public void LoadOrderForEdit(Order order)
        {
            _editingOrder = order;
            IsEditMode = true;

            // Set OrderType first so IsTableless is correct before TableNumber is set
            OrderType = string.IsNullOrWhiteSpace(order.OrderType) ? OrderTypes.DineIn : order.OrderType;

            // For tableless orders the stored TableNumber is 0 — keep it as-is
            // For DineIn, use the stored table number (always > 0)
            TableNumber = IsTableless ? 0 : order.TableNumber;

            _cartService.LoadItems(TableNumber, order.Items);
            UpdateCart(); // Calculate Subtotal before setting DiscountAmount
            DiscountAmount = order.DiscountAmount;
            PaymentMode = order.PaymentMode;
            CustomerName = order.CustomerName;
            CustomerPhone = order.CustomerPhone;
            CustomerGstin = order.CustomerGstin;
            CustomerId = order.CustomerId;
            UpdateCart();
            StatusMessage = $"✏ Editing Order #{order.Id}";
        }

        [RelayCommand]
        private void CancelEdit()
        {
            if (_editingOrder != null) _cartService.Clear(TableNumber);
            _editingOrder = null;
            IsEditMode = false;
            OrderType = OrderTypes.DineIn;   // reset to default
            UpdateCart();
            StatusMessage = "Ready";
            OrderEditCancelled?.Invoke();
        }

        [RelayCommand]
        private void ToggleOrderType()
        {
            if (OrderType == OrderTypes.DineIn) OrderType = OrderTypes.Takeaway;
            else if (OrderType == OrderTypes.Takeaway) OrderType = OrderTypes.Online;
            else OrderType = OrderTypes.DineIn;
        }

        /// <summary>
        /// Persists the current cart as an order (updates an existing order when editing or creates a new order), triggers printing if applicable, and clears checkout state on success.
        /// </summary>
        /// <returns>A task that completes when the save (or update), any printing, and subsequent cart/state cleanup have finished.</returns>
        [RelayCommand]
        private async Task SaveOrderAsync()
        {
            var rawItems = _cartService.GetItems(TableNumber);
            if (!await ValidateCheckoutAsync(rawItems)) return;

            var (proceed, finalCash, finalCard, finalUpi, finalPaymentMode) = await ResolveCheckoutPaymentAsync(rawItems);
            if (!proceed)
            {
                CheckoutCancelled?.Invoke();
                return;
            }

            try
            {
                var (saved, orderId) = await PersistOrderAsync(rawItems, finalPaymentMode, finalCash, finalCard, finalUpi);
                if (!saved) return;

                // Print receipt for new orders (edits don't trigger printing)
                if (!IsEditMode && orderId > 0)
                {
                    await PrintWithRetryAsync(orderId);
                }

                var wasEditMode = IsEditMode;
                ResetAfterSave();

                // Fire after state is fully cleared so the dashboard refreshes
                // with a clean VM — only for updates, not new orders
                if (wasEditMode)
                    OrderUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Save failed: {ex.Message}");
            }
        }

        // LOOPHOLE FIX: empty cart / missing payment mode / shift-closed guards
        private async Task<bool> ValidateCheckoutAsync(List<OrderItem> rawItems)
        {
            if (rawItems.Count == 0)
            {
                _notificationService.ShowInfo("Cannot save empty order");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PaymentMode))
            {
                _notificationService.ShowError("Please select a payment mode before checkout.");
                return false;
            }

            // Dine In orders must be tied to a real table (server rejects TableNumber <= 0 for
            // DineIn); catch it here with an actionable message instead of surfacing the backend
            // validator's raw "Invalid table number." exception.
            if (!IsTableless && TableNumber <= 0)
            {
                _notificationService.ShowError("Please select a table before checkout.");
                return false;
            }

            using (var scope = App.CreateDbScope())
            {
                var cashService = scope.ServiceProvider.GetRequiredService<ICashService>();
                var currentSession = await cashService.GetCurrentSessionAsync();
                if (currentSession == null)
                {
                    _notificationService.ShowError("Shift is not open. Please open a shift before checkout.");
                    return false;
                }
            }

            return true;
        }

        // Shows the Confirm Checkout dialog (if available) and resolves the final payment split.
        private async Task<(bool Proceed, decimal Cash, decimal Card, decimal Upi, string PaymentMode)> ResolveCheckoutPaymentAsync(List<OrderItem> rawItems)
        {
            decimal finalCash = 0;
            decimal finalCard = 0;
            decimal finalUpi = 0;
            string finalPaymentMode = PaymentMode;

            if (_dialogService == null) return (true, finalCash, finalCard, finalUpi, finalPaymentMode);

            var details = new ConfirmCheckoutDetails
            {
                TotalItems = rawItems.Sum(i => i.Quantity),
                TotalAmount = Subtotal + GstAmount,
                DiscountAmount = DiscountAmount,
                FinalPayableAmount = TotalAmount,
                PaymentMode = PaymentMode
            };

            bool confirmed = await _dialogService.ShowConfirmCheckoutAsync(details);
            if (!confirmed) return (false, finalCash, finalCard, finalUpi, finalPaymentMode);

            finalPaymentMode = details.PaymentMode;
            if (finalPaymentMode == "Split")
            {
                finalCash = details.CashAmount;
                finalCard = details.CardAmount;
                finalUpi = details.UpiAmount;
            }

            return (true, finalCash, finalCard, finalUpi, finalPaymentMode);
        }

        // Re-verifies the shift under lock, then updates or creates the order.
        // Returns (false, 0) if the shift closed between the pre-check and now.
        // For new orders returns the persisted order ID so the caller can trigger printing separately.
        private async Task<(bool Saved, int OrderId)> PersistOrderAsync(List<OrderItem> rawItems, string finalPaymentMode, decimal finalCash, decimal finalCard, decimal finalUpi)
        {
            using (var scope = App.CreateDbScope())
            {
                var cashService = scope.ServiceProvider.GetRequiredService<ICashService>();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                var currentSession = await cashService.GetCurrentSessionAsync();
                if (currentSession == null)
                {
                    _notificationService.ShowError("Shift is not open. Please open a shift before checkout.");
                    return (false, 0);
                }

                if (IsEditMode && _editingOrder != null)
                {
                    await UpdateEditingOrderAsync(orderService, rawItems, finalPaymentMode, finalCash, finalCard, finalUpi);
                }
                else
                {
                    int orderId;
                    if (finalPaymentMode == PaymentModes.Split)
                    {
                        orderId = await CreateSplitPaymentOrderAsync(orderService, rawItems, finalCash, finalCard, finalUpi);
                    }
                    else
                    {
                        orderId = await orderService.SaveOrderAsync(new SaveOrderRequest(rawItems, TableNumber, DiscountAmount, finalPaymentMode, CustomerName, CustomerPhone, CustomerGstin, OrderType, CustomerId));
                    }

                    return (true, orderId);
                }
            }

            return (true, 0);
        }

        private async Task UpdateEditingOrderAsync(IOrderService orderService, List<OrderItem> rawItems, string finalPaymentMode, decimal finalCash, decimal finalCard, decimal finalUpi)
        {
            _editingOrder!.Items = rawItems;
            _editingOrder.TableNumber = TableNumber;
            _editingOrder.DiscountAmount = DiscountAmount;
            _editingOrder.PaymentMode = finalPaymentMode;
            _editingOrder.OrderType = OrderType;
            _editingOrder.CustomerName = CustomerName;
            _editingOrder.CustomerPhone = CustomerPhone;
            _editingOrder.CustomerGstin = CustomerGstin;
            _editingOrder.CustomerId = CustomerId;

            if (finalPaymentMode == PaymentModes.Split)
            {
                _editingOrder.CashPaid = finalCash;
                _editingOrder.CardPaid = finalCard;
                _editingOrder.UpiPaid = finalUpi;
                _editingOrder.AmountPaid = finalCash + finalCard + finalUpi;
                _editingOrder.Status = _editingOrder.AmountPaid >= _editingOrder.TotalAmount ? OrderStatuses.Paid : OrderStatuses.Partial;
            }
            else
            {
                _editingOrder.AmountPaid = _editingOrder.TotalAmount;
                _editingOrder.CashPaid = finalPaymentMode == PaymentModes.Cash ? _editingOrder.TotalAmount : 0;
                _editingOrder.CardPaid = finalPaymentMode == PaymentModes.Card ? _editingOrder.TotalAmount : 0;
                _editingOrder.UpiPaid = finalPaymentMode == PaymentModes.Upi ? _editingOrder.TotalAmount : 0;
                _editingOrder.Status = OrderStatuses.Paid;
            }

            await orderService.UpdateOrderAsync(_editingOrder);
        }

        // Save order first (initially cash mode to bypass validation), then apply the real split payment details.
        private async Task<int> CreateSplitPaymentOrderAsync(IOrderService orderService, List<OrderItem> rawItems, decimal finalCash, decimal finalCard, decimal finalUpi) // NOSONAR
        {
            int orderId = await orderService.SaveOrderAsync(new SaveOrderRequest(rawItems, TableNumber, DiscountAmount, PaymentModes.Cash, CustomerName, CustomerPhone, CustomerGstin, OrderType, CustomerId));
            var createdOrder = await orderService.GetOrderAsync(orderId);
            if (createdOrder != null)
            {
                createdOrder.PaymentMode = PaymentModes.Split;
                createdOrder.CashPaid = finalCash;
                createdOrder.CardPaid = finalCard;
                createdOrder.UpiPaid = finalUpi;
                createdOrder.AmountPaid = finalCash + finalCard + finalUpi;
                createdOrder.Status = createdOrder.AmountPaid >= createdOrder.TotalAmount ? OrderStatuses.Paid : OrderStatuses.Partial;
                await orderService.UpdateOrderAsync(createdOrder);
            }
            return orderId;
        }

        // Clears cart/checkout state automatically without prompting the user.
        private void ResetAfterSave()
        {
            _cartService.Clear(TableNumber);
            DiscountAmount = 0;
            UpdateCart();
            CartCleared?.Invoke();

            IsEditMode = false;
            _editingOrder = null;
            PaymentMode = PaymentModes.Cash;
            OrderType = OrderTypes.DineIn;
            // CLEANUP: Reset customer details to null to align with property definitions and save memory
            CustomerName = null;
            CustomerPhone = null;
            CustomerGstin = null;
            CustomerId = null;
        }

        // Any manual edit of the phone number invalidates a previously resolved customer link,
        // so the cashier must re-run the lookup (or explicitly save a new profile) before it applies again.
        partial void OnCustomerPhoneChanged(string? value)
        {
            CustomerId = null;
        }

        /// <summary>Looks up a saved CRM customer profile by phone and, if found, links the order and fills in Name/GSTIN.</summary>
        [RelayCommand]
        private async Task LookupCustomerAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomerPhone))
            {
                _notificationService.ShowInfo("Enter a phone number to look up a customer.");
                return;
            }

            using var scope = App.CreateDbScope();
            var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
            var customer = await customerService.GetCustomerByPhoneAsync(CustomerPhone);

            if (customer == null)
            {
                _notificationService.ShowInfo("No matching customer profile found. It will be saved as a walk-in order.");
                return;
            }

            CustomerId = customer.Id;
            CustomerName = customer.Name;
            CustomerGstin = customer.Gstin;
            _notificationService.ShowSuccess($"Linked to customer '{customer.Name}'.");
        }

        /// <summary>
        /// Attempts to print the receipt for a saved order. On failure, offers the user
        /// a retry dialog so the order isn't silently left without a printed receipt.
        /// </summary>
        private async Task PrintWithRetryAsync(int orderId)
        {
            bool printed = await PrintOrderAsync(orderId);
            while (!printed && _dialogService != null)
            {
                var retry = await _dialogService.ShowMessageAsync(
                    $"Receipt printing failed for Order #{orderId}.\n\nThe order has been saved successfully.\nWould you like to retry printing?",
                    "Print Failed \u2013 Retry?",
                    DialogButton.YesNo,
                    DialogIcon.Warning);
                if (retry != DialogResult.Yes) break;
                printed = await PrintOrderAsync(orderId);
            }
        }

        /// <summary>
        /// Generates a receipt for the specified order and either shows a print preview or sends it to the printer.
        /// Returns true if printing succeeded, false otherwise.
        /// </summary>
        /// <param name="orderId">The identifier of the order to print if <paramref name="preLoadedOrder"/> is not provided.</param>
        /// <param name="preLoadedOrder">An optional preloaded <see cref="Order"/> to use instead of loading the order by <paramref name="orderId"/>.</param>
        /// <param name="skipPreview">When true, bypasses the print preview even if previewing is enabled in settings and prints directly.</param>
        private async Task<bool> PrintOrderAsync(int orderId, Order? preLoadedOrder = null, bool skipPreview = false)
        {
            try
            {
                SystemSetting settings;
                Order? order;
                using (var scope = App.CreateDbScope())
                {
                    var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    settings = await settingService.GetSettingsAsync();
                    order = preLoadedOrder ?? await orderService.GetOrderAsync(orderId);
                }

                if (order == null) return false;

                // Execute on UI thread for printing
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => PrintOrderOnUiThread(order, settings, skipPreview));
                }

                return true;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Print failed: {ex.Message}");
                return false;
            }
        }

        private void PrintOrderOnUiThread(Order order, SystemSetting settings, bool skipPreview)
        {
            var doc = ReceiptGenerator.CreateReceipt(order, settings.ReceiptFormat == "Thermal", settings);
            if (settings.ShowPrintPreview && !skipPreview)
            {
                var preview = new PrintPreviewWindow(order, settings);
                preview.ShowDialog();
                PrintPreviewClosed?.Invoke();
                return;
            }

            var dialog = new System.Windows.Controls.PrintDialog();
            if (!string.IsNullOrEmpty(settings.DefaultPrinter))
            {
                try
                {
                    dialog.PrintQueue = new System.Printing.LocalPrintServer().GetPrintQueue(settings.DefaultPrinter);
                }
                catch { /* Fallback to default if printer not found */ }
            }
            dialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, "Receipt " + order.Id);
        }
    }
}
