using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HotelPOS.ViewModels
{
    public partial class PurchaseEntryViewModel : ObservableObject
    {
        private readonly IPurchaseService _purchaseService;
        private readonly IItemService _itemService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private Supplier? _selectedSupplier;

        [ObservableProperty]
        private string _invoiceNumber = string.Empty;

        [ObservableProperty]
        private DateTime _purchaseDate = DateTime.Now;

        [ObservableProperty]
        private string _paymentType = "Cash"; // Cash, Credit, UPI

        [ObservableProperty]
        private string? _notes;

        [ObservableProperty]
        private decimal _subtotal;

        [ObservableProperty]
        private decimal _totalTax;

        [ObservableProperty]
        private decimal _totalDiscount;

        [ObservableProperty]
        private decimal _grandTotal;

        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public ObservableCollection<Item> CatalogItems { get; } = new();
        public ObservableCollection<PurchaseRow> PurchaseRows { get; } = new();

        public PurchaseEntryViewModel(
            IPurchaseService purchaseService,
            IItemService itemService,
            INotificationService notificationService)
        {
            _purchaseService = purchaseService;
            _itemService = itemService;
            _notificationService = notificationService;

            PurchaseRows.CollectionChanged += PurchaseRows_CollectionChanged;

            // Load suppliers and items in a non-blocking task
            _ = LoadDataAsync();
        }

        /// <summary>
        /// Loads suppliers and catalog items into the view model and ensures there is at least one purchase row.
        /// </summary>
        /// <returns>A task that completes after Suppliers and CatalogItems are populated and PurchaseRows contains at least one row.</returns>
        public async Task LoadDataAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var purchaseService = scope.ServiceProvider.GetService<IPurchaseService>() ?? _purchaseService;
                var itemService = scope.ServiceProvider.GetService<IItemService>() ?? _itemService;
                try
                {
                    Suppliers.Clear();
                    var suppliers = await purchaseService.GetSuppliersAsync();
                    foreach (var sup in suppliers)
                    {
                        Suppliers.Add(sup);
                    }

                    CatalogItems.Clear();
                    var items = await itemService.GetItemsAsync();
                    foreach (var it in items)
                    {
                        CatalogItems.Add(it);
                    }

                    // Initialize with one empty row if none exist
                    if (PurchaseRows.Count == 0)
                    {
                        AddRow();
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load catalog data: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Adds a new empty purchase row to the PurchaseRows collection.
        /// </summary>
        [RelayCommand]
        private void AddRow()
        {
            var newRow = new PurchaseRow();
            PurchaseRows.Add(newRow);
        }

        [RelayCommand]
        private void RemoveRow(PurchaseRow? row)
        {
            if (row != null && PurchaseRows.Contains(row))
            {
                PurchaseRows.Remove(row);
                
                // Keep at least one empty row for user convenience
                if (PurchaseRows.Count == 0)
                {
                    AddRow();
                }
            }
        }

        /// <summary>
        /// Validates the current purchase form, persists the purchase to the data store, and resets the form on success.
        /// </summary>
        /// <remarks>
        /// Performs form- and row-level validations, shows warning notifications for validation failures, saves a constructed Purchase (including its PurchaseItems) via the application-scoped IPurchaseService, shows a success notification on completion, and shows an error notification if saving fails.
        /// </remarks>
        /// <returns>A task that completes when the save operation and its notifications have finished.</returns>
        [RelayCommand]
        private async Task SavePurchaseAsync()
        {
            try
            {
                // 1. Form Level Validations
                if (SelectedSupplier == null)
                {
                    _notificationService.ShowWarning("Please select a supplier.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(InvoiceNumber))
                {
                    _notificationService.ShowWarning("Invoice Number is required.");
                    return;
                }

                // Filter out empty rows (where ItemId is not set)
                var activeRows = PurchaseRows.Where(r => r.ItemId > 0).ToList();
                if (!activeRows.Any())
                {
                    _notificationService.ShowWarning("Please add at least one item to the purchase.");
                    return;
                }

                // 2. Row Level Validations
                foreach (var row in activeRows)
                {
                    if (row.Quantity <= 0)
                    {
                        _notificationService.ShowWarning($"Quantity for '{row.ItemName}' must be greater than zero.");
                        return;
                    }

                    if (row.UnitPrice < 0)
                    {
                        _notificationService.ShowWarning($"Unit Price for '{row.ItemName}' cannot be negative.");
                        return;
                    }

                    if (row.Discount < 0)
                    {
                        _notificationService.ShowWarning($"Discount for '{row.ItemName}' cannot be negative.");
                        return;
                    }
                }

                // Create Domain Models
                var purchase = new Purchase
                {
                    SupplierId = SelectedSupplier.Id,
                    InvoiceNumber = InvoiceNumber.Trim(),
                    PurchaseDate = PurchaseDate,
                    PaymentType = PaymentType,
                    Notes = Notes?.Trim(),
                    Subtotal = Subtotal,
                    TotalTax = TotalTax,
                    TotalDiscount = TotalDiscount,
                    GrandTotal = GrandTotal,
                    PurchaseItems = activeRows.Select(row => new PurchaseItem
                    {
                        ItemId = row.ItemId,
                        ItemName = row.ItemName,
                        Quantity = row.Quantity,
                        UnitPrice = row.UnitPrice,
                        TaxPercentage = row.TaxPercentage,
                        Discount = row.Discount,
                        Total = row.Total
                    }).ToList()
                };

                using (var scope = App.CreateDbScope())
                {
                    var purchaseService = scope.ServiceProvider.GetService<IPurchaseService>() ?? _purchaseService;
                    await purchaseService.SavePurchaseAsync(purchase);
                }
                _notificationService.ShowSuccess("Purchase entry saved successfully and stock quantities updated.");

                // Reset form fields
                ResetForm();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to save purchase: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
        }

        private void ResetForm()
        {
            SelectedSupplier = null;
            InvoiceNumber = string.Empty;
            PurchaseDate = DateTime.Now;
            PaymentType = "Cash";
            Notes = string.Empty;
            PurchaseRows.Clear();
            AddRow(); // Pre-fill with a single empty row
        }

        private void PurchaseRows_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PurchaseRow row in e.OldItems)
                {
                    row.PropertyChanged -= Row_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (PurchaseRow row in e.NewItems)
                {
                    row.PropertyChanged += Row_PropertyChanged;
                }
            }
            RecalculateTotals();
            UpdateSNo();
        }

        private void Row_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is PurchaseRow row)
            {
                if (e.PropertyName == nameof(PurchaseRow.ItemId))
                {
                    // Item changed, prefill defaults from catalog
                    var item = CatalogItems.FirstOrDefault(i => i.Id == row.ItemId);
                    if (item != null)
                    {
                        // Check duplicate first
                        if (PurchaseRows.Any(r => r != row && r.ItemId == row.ItemId))
                        {
                            _notificationService.ShowWarning($"Item '{item.Name}' is already added to the purchase.");
                            row.ItemId = 0;
                            row.ItemName = string.Empty;
                            row.UnitPrice = 0;
                            row.TaxPercentage = 0;
                            row.Discount = 0;
                            row.Total = 0;
                            return;
                        }
                        
                        row.ItemName = item.Name;
                        row.UnitPrice = item.Price;
                        row.TaxPercentage = item.TaxPercentage;
                        
                        // Recalculate row total
                        var sub = row.Quantity * row.UnitPrice;
                        var tax = Math.Round(sub * (row.TaxPercentage / 100m), 2);
                        row.Total = sub + tax - row.Discount;
                    }
                }

                if (e.PropertyName == nameof(PurchaseRow.Quantity) ||
                    e.PropertyName == nameof(PurchaseRow.UnitPrice) ||
                    e.PropertyName == nameof(PurchaseRow.TaxPercentage) ||
                    e.PropertyName == nameof(PurchaseRow.Discount))
                {
                    // Recalculate row total
                    var sub = row.Quantity * row.UnitPrice;
                    var tax = Math.Round(sub * (row.TaxPercentage / 100m), 2);
                    row.Total = sub + tax - row.Discount;
                    
                    RecalculateTotals();
                }
            }
        }

        private void UpdateSNo()
        {
            for (int i = 0; i < PurchaseRows.Count; i++)
            {
                PurchaseRows[i].SNo = i + 1;
            }
        }

        private void RecalculateTotals()
        {
            decimal subtotal = 0;
            decimal totalTax = 0;
            decimal totalDiscount = 0;
            decimal grandTotal = 0;

            foreach (var row in PurchaseRows)
            {
                if (row.ItemId > 0)
                {
                    var rowSub = row.Quantity * row.UnitPrice;
                    var rowTax = Math.Round(rowSub * (row.TaxPercentage / 100m), 2);

                    subtotal += rowSub;
                    totalTax += rowTax;
                    totalDiscount += row.Discount;
                    grandTotal += row.Total;
                }
            }

            Subtotal = subtotal;
            TotalTax = totalTax;
            TotalDiscount = totalDiscount;
            GrandTotal = grandTotal;
        }
    }
}
