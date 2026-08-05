using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class EstimationViewModel : ObservableObject
    {
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private Customer? _selectedCustomer;

        [ObservableProperty]
        private string _estimationNumber = string.Empty;

        [ObservableProperty]
        private DateTime _estimationDate = DateTime.Now;

        [ObservableProperty]
        private DateTime? _validUntil;

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

        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Item> CatalogItems { get; } = new();
        public ObservableCollection<EstimationRow> EstimationRows { get; } = new();
        public ObservableCollection<Estimation> Estimations { get; } = new();

        public Task InitializationTask { get; }

        public EstimationViewModel(
            IEstimationService estimationService,
            ICustomerService customerService,
            IItemService itemService,
            INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(estimationService);
                App.RegisterTestService(customerService);
                App.RegisterTestService(itemService);
                App.RegisterTestService(notificationService);
            }

            EstimationRows.CollectionChanged += EstimationRows_CollectionChanged;

            // Load customers/items/estimations in a non-blocking task
            InitializationTask = LoadDataAsync();
        }

        /// <summary>
        /// Loads customers, catalog items, and existing estimations into the view model and
        /// ensures there is at least one item row for the entry form.
        /// </summary>
        public async Task LoadDataAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var estimationService = scope.ServiceProvider.GetRequiredService<IEstimationService>();
                var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
                var itemService = scope.ServiceProvider.GetRequiredService<IItemService>();
                try
                {
                    Customers.Clear();
                    var customers = await customerService.GetCustomersAsync();
                    foreach (var c in customers)
                    {
                        Customers.Add(c);
                    }

                    CatalogItems.Clear();
                    var items = await itemService.GetItemsAsync();
                    foreach (var it in items)
                    {
                        CatalogItems.Add(it);
                    }

                    await RefreshEstimationsAsync(estimationService);

                    if (EstimationRows.Count == 0)
                    {
                        AddRow();
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load estimation data: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task RefreshEstimationsAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var estimationService = scope.ServiceProvider.GetRequiredService<IEstimationService>();
                await RefreshEstimationsAsync(estimationService);
            }
        }

        private async Task RefreshEstimationsAsync(IEstimationService estimationService)
        {
            try
            {
                var estimations = await estimationService.GetEstimationsAsync();
                Estimations.Clear();
                foreach (var e in estimations)
                {
                    Estimations.Add(e);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load estimations: {ex.Message}");
            }
        }

        [RelayCommand]
        private void AddRow()
        {
            EstimationRows.Add(new EstimationRow());
        }

        [RelayCommand]
        private void RemoveRow(EstimationRow? row)
        {
            if (row != null && EstimationRows.Contains(row))
            {
                EstimationRows.Remove(row);

                if (EstimationRows.Count == 0)
                {
                    AddRow();
                }
            }
        }

        [RelayCommand]
        private async Task SaveEstimationAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EstimationNumber))
                {
                    _notificationService.ShowWarning("Estimation Number is required.");
                    return;
                }

                var activeRows = EstimationRows.Where(r => r.ItemId > 0).ToList();
                if (!activeRows.Any())
                {
                    _notificationService.ShowWarning("Please add at least one item to the estimation.");
                    return;
                }

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

                var estimation = new Estimation
                {
                    EstimationNumber = EstimationNumber.Trim(),
                    EstimationDate = EstimationDate,
                    ValidUntil = ValidUntil,
                    CustomerId = SelectedCustomer?.Id,
                    CustomerName = SelectedCustomer?.Name,
                    CustomerPhone = SelectedCustomer?.Phone,
                    Status = EstimationStatuses.Draft,
                    Notes = Notes?.Trim(),
                    Subtotal = Subtotal,
                    TotalTax = TotalTax,
                    TotalDiscount = TotalDiscount,
                    GrandTotal = GrandTotal,
                    EstimationItems = activeRows.Select(row => new EstimationItem
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
                    var estimationService = scope.ServiceProvider.GetRequiredService<IEstimationService>();
                    await estimationService.SaveEstimationAsync(estimation);
                    await RefreshEstimationsAsync(estimationService);
                }
                _notificationService.ShowSuccess("Estimation saved successfully.");

                ResetForm();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to save estimation: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
        }

        [RelayCommand]
        private Task MarkAsSentAsync(Estimation? estimation) => ChangeStatusAsync(estimation, EstimationStatuses.Sent);

        [RelayCommand]
        private Task MarkAsAcceptedAsync(Estimation? estimation) => ChangeStatusAsync(estimation, EstimationStatuses.Accepted);

        [RelayCommand]
        private Task MarkAsRejectedAsync(Estimation? estimation) => ChangeStatusAsync(estimation, EstimationStatuses.Rejected);

        private async Task ChangeStatusAsync(Estimation? estimation, string newStatus)
        {
            if (estimation == null || IsBusy) return;

            IsBusy = true;
            try
            {
                // Send a copy with the new status rather than mutating the shared instance
                // still bound to the Estimations grid - if UpdateEstimationAsync throws, the
                // bound object (and therefore the UI) must keep showing the last-confirmed
                // status, not one the server never actually accepted.
                var request = new Estimation
                {
                    Id = estimation.Id,
                    EstimationNumber = estimation.EstimationNumber,
                    EstimationDate = estimation.EstimationDate,
                    ValidUntil = estimation.ValidUntil,
                    CustomerId = estimation.CustomerId,
                    CustomerName = estimation.CustomerName,
                    CustomerPhone = estimation.CustomerPhone,
                    Status = newStatus,
                    Notes = estimation.Notes,
                    Subtotal = estimation.Subtotal,
                    TotalTax = estimation.TotalTax,
                    TotalDiscount = estimation.TotalDiscount,
                    GrandTotal = estimation.GrandTotal,
                    ConvertedOrderId = estimation.ConvertedOrderId,
                    EstimationItems = estimation.EstimationItems
                };

                using (var scope = App.CreateDbScope())
                {
                    var estimationService = scope.ServiceProvider.GetRequiredService<IEstimationService>();
                    await estimationService.UpdateEstimationAsync(request);
                    await RefreshEstimationsAsync(estimationService); // repopulates Estimations with confirmed server state
                }
                _notificationService.ShowSuccess($"Estimation '{estimation.EstimationNumber}' marked as {newStatus}.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to update estimation status: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ConvertToOrderAsync(Estimation? estimation)
        {
            if (estimation == null || IsBusy) return;

            IsBusy = true;
            try
            {
                int orderId;
                using (var scope = App.CreateDbScope())
                {
                    var estimationService = scope.ServiceProvider.GetRequiredService<IEstimationService>();
                    orderId = await estimationService.ConvertToOrderAsync(estimation.Id);
                    await RefreshEstimationsAsync(estimationService);
                }
                _notificationService.ShowSuccess($"Estimation '{estimation.EstimationNumber}' converted to Order #{orderId}.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to convert estimation to an order: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ResetForm()
        {
            SelectedCustomer = null;
            EstimationNumber = string.Empty;
            EstimationDate = DateTime.Now;
            ValidUntil = null;
            Notes = string.Empty;
            EstimationRows.Clear();
            AddRow();
        }

        private void EstimationRows_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (EstimationRow row in e.OldItems)
                {
                    row.PropertyChanged -= Row_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (EstimationRow row in e.NewItems)
                {
                    row.PropertyChanged += Row_PropertyChanged;
                }
            }
            RecalculateTotals();
            UpdateSNo();
        }

        private void Row_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is EstimationRow row)
            {
                if (e.PropertyName == nameof(EstimationRow.ItemId))
                {
                    var item = CatalogItems.FirstOrDefault(i => i.Id == row.ItemId);
                    if (item != null)
                    {
                        if (EstimationRows.Any(r => r != row && r.ItemId == row.ItemId))
                        {
                            _notificationService.ShowWarning($"Item '{item.Name}' is already added to the estimation.");
                            row.ItemId = 0;
                            row.ItemName = string.Empty;
                            row.Unit = "Pcs";
                            row.UnitPrice = 0;
                            row.TaxPercentage = 0;
                            row.Discount = 0;
                            row.Total = 0;
                            return;
                        }

                        row.ItemName = item.Name;
                        row.Unit = item.Unit?.Name ?? "Pcs";
                        row.UnitPrice = item.Price;
                        row.TaxPercentage = item.TaxPercentage;

                        var sub = row.Quantity * row.UnitPrice;
                        var tax = Math.Round(sub * (row.TaxPercentage / 100m), 2);
                        row.Total = sub + tax - row.Discount;
                    }
                }

                if (e.PropertyName == nameof(EstimationRow.Quantity) ||
                    e.PropertyName == nameof(EstimationRow.UnitPrice) ||
                    e.PropertyName == nameof(EstimationRow.TaxPercentage) ||
                    e.PropertyName == nameof(EstimationRow.Discount))
                {
                    var sub = row.Quantity * row.UnitPrice;
                    var tax = Math.Round(sub * (row.TaxPercentage / 100m), 2);
                    row.Total = sub + tax - row.Discount;

                    RecalculateTotals();
                }
            }
        }

        private void UpdateSNo()
        {
            for (int i = 0; i < EstimationRows.Count; i++)
            {
                EstimationRows[i].SNo = i + 1;
            }
        }

        private void RecalculateTotals()
        {
            decimal subtotal = 0;
            decimal totalTax = 0;
            decimal totalDiscount = 0;
            decimal grandTotal = 0;

            foreach (var row in EstimationRows)
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
