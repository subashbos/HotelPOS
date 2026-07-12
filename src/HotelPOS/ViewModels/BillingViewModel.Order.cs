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
            if (rawItems.Count == 0)
            {
                _notificationService.ShowInfo("Cannot save empty order");
                return;
            }

            // LOOPHOLE FIX: Validate PaymentMode
            if (string.IsNullOrWhiteSpace(PaymentMode))
            {
                _notificationService.ShowError("Please select a payment mode before checkout.");
                return;
            }

            // LOOPHOLE FIX: Prevent checkout if shift is closed
            using (var scope = App.CreateDbScope())
            {
                var cashService = scope.ServiceProvider.GetRequiredService<ICashService>();
                var currentSession = await cashService.GetCurrentSessionAsync();
                if (currentSession == null)
                {
                    _notificationService.ShowError("Shift is not open. Please open a shift before checkout.");
                    return;
                }
            }

            decimal finalCash = 0;
            decimal finalCard = 0;
            decimal finalUpi = 0;
            string finalPaymentMode = PaymentMode;

            // Show Confirm Checkout Dialog if service is available
            if (_dialogService != null)
            {
                var details = new ConfirmCheckoutDetails
                {
                    TotalItems = rawItems.Sum(i => i.Quantity),
                    TotalAmount = Subtotal + GstAmount,
                    DiscountAmount = DiscountAmount,
                    FinalPayableAmount = TotalAmount,
                    PaymentMode = PaymentMode
                };

                bool confirmed = await _dialogService.ShowConfirmCheckoutAsync(details);
                if (!confirmed)
                {
                    CheckoutCancelled?.Invoke();
                    return;
                }

                finalPaymentMode = details.PaymentMode;
                if (finalPaymentMode == "Split")
                {
                    finalCash = details.CashAmount;
                    finalCard = details.CardAmount;
                    finalUpi = details.UpiAmount;
                }
            }

            // Perform the save
            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var cashService = scope.ServiceProvider.GetRequiredService<ICashService>();
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                    // Re-verify shift state under lock
                    var currentSession = await cashService.GetCurrentSessionAsync();
                    if (currentSession == null)
                    {
                        _notificationService.ShowError("Shift is not open. Please open a shift before checkout.");
                        return;
                    }

                    if (IsEditMode && _editingOrder != null)
                    {
                        _editingOrder.Items = rawItems;
                        _editingOrder.TableNumber = TableNumber;
                        _editingOrder.DiscountAmount = DiscountAmount;
                        _editingOrder.PaymentMode = finalPaymentMode;
                        _editingOrder.OrderType = OrderType;
                        _editingOrder.CustomerName = CustomerName;
                        _editingOrder.CustomerPhone = CustomerPhone;
                        _editingOrder.CustomerGstin = CustomerGstin;

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
                    else
                    {
                        int orderId;
                        if (finalPaymentMode == PaymentModes.Split)
                        {
                            // Save order first (initially cash mode to bypass validate, then updates details)
                            orderId = await orderService.SaveOrderAsync(new SaveOrderRequest(rawItems, TableNumber, DiscountAmount, PaymentModes.Cash, CustomerName, CustomerPhone, CustomerGstin, OrderType));
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
                        }
                        else
                        {
                            orderId = await orderService.SaveOrderAsync(new SaveOrderRequest(rawItems, TableNumber, DiscountAmount, finalPaymentMode, CustomerName, CustomerPhone, CustomerGstin, OrderType));
                        }

                        // Trigger Print
                        await PrintOrderAsync(orderId);
                    }
                }

                var wasEditMode = IsEditMode;

                // Clear cart automatically without prompting the user
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

        /// <summary>
        /// Generates a receipt for the specified order and either shows a print preview or sends it to the printer.
        /// </summary>
        /// <param name="orderId">The identifier of the order to print if <paramref name="preLoadedOrder"/> is not provided.</param>
        /// <param name="preLoadedOrder">An optional preloaded <see cref="Order"/> to use instead of loading the order by <paramref name="orderId"/>.</param>
        /// <param name="skipPreview">When true, bypasses the print preview even if previewing is enabled in settings and prints directly.</param>
        private async Task PrintOrderAsync(int orderId, Order? preLoadedOrder = null, bool skipPreview = false)
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

                if (order == null) return;

                // Execute on UI thread for printing
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var doc = ReceiptGenerator.CreateReceipt(order, settings.ReceiptFormat == "Thermal", settings);
                        if (settings.ShowPrintPreview && !skipPreview)
                        {
                            var preview = new PrintPreviewWindow(order, settings);
                            preview.ShowDialog();
                            PrintPreviewClosed?.Invoke();
                        }
                        else
                        {
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
                    });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Print failed: {ex.Message}");
            }
        }
    }
}
