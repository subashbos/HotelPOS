using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class CustomerViewModel : ObservableObject
    {
        private readonly INotificationService _notificationService;
        private readonly List<Customer> _allCustomers = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        private Customer? _selectedCustomer;
        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public ObservableCollection<Customer> Customers { get; } = new();

        // Callback to show the entry dialog (Add/Edit)
        public Func<Customer?, Task<bool>>? ShowEntryDialogAsync { get; set; }

        // Callback to show the order-history dialog
        public Func<Customer, Task>? ShowHistoryAsync { get; set; }

        // Callback for confirming deactivation
        public Func<string, Task<bool>>? ConfirmDeactivateAsync { get; set; }

        public CustomerViewModel(ICustomerService customerService, INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(customerService);
                App.RegisterTestService(notificationService);
            }
        }

        public async Task LoadCustomersAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
                try
                {
                    var customers = await customerService.GetCustomersAsync();
                    _allCustomers.Clear();
                    _allCustomers.AddRange(customers);
                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load customers: {ex.Message}");
                }
            }
        }

        private void ApplyFilter()
        {
            Customers.Clear();
            var search = SearchText.Trim().ToLower();

            var filtered = _allCustomers.Where(c =>
                string.IsNullOrEmpty(search) ||
                c.Name.ToLower().Contains(search) ||
                (c.Phone != null && c.Phone.Contains(search)) ||
                (c.Email != null && c.Email.ToLower().Contains(search)) ||
                (c.Gstin != null && c.Gstin.ToLower().Contains(search))
            );

            foreach (var customer in filtered)
            {
                Customers.Add(customer);
            }
        }

        [RelayCommand]
        private async Task AddCustomerAsync()
        {
            if (ShowEntryDialogAsync != null)
            {
                var success = await ShowEntryDialogAsync(null);
                if (success)
                {
                    await LoadCustomersAsync();
                }
            }
        }

        [RelayCommand]
        private async Task EditCustomerAsync(Customer? customer)
        {
            var target = customer ?? SelectedCustomer;
            if (target == null)
            {
                _notificationService.ShowWarning("Please select a customer to edit.");
                return;
            }

            if (ShowEntryDialogAsync != null)
            {
                var success = await ShowEntryDialogAsync(target);
                if (success)
                {
                    await LoadCustomersAsync();
                }
            }
        }

        [RelayCommand]
        private async Task ViewHistoryAsync(Customer? customer)
        {
            var target = customer ?? SelectedCustomer;
            if (target == null)
            {
                _notificationService.ShowWarning("Please select a customer to view history.");
                return;
            }

            if (ShowHistoryAsync != null)
            {
                await ShowHistoryAsync(target);
            }
        }

        /// <summary>Deactivates the specified customer (or the currently selected one) after confirmation. Deactivated customers are hidden from the active list but their order history is preserved.</summary>
        [RelayCommand]
        private async Task DeleteCustomerAsync(Customer? customer)
        {
            var target = customer ?? SelectedCustomer;
            if (target == null)
            {
                _notificationService.ShowWarning("Please select a customer to deactivate.");
                return;
            }

            if (ConfirmDeactivateAsync != null)
            {
                var confirmed = await ConfirmDeactivateAsync(target.Name);
                if (confirmed)
                {
                    using (var scope = App.CreateDbScope())
                    {
                        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
                        try
                        {
                            await customerService.DeleteCustomerAsync(target.Id);
                            _notificationService.ShowSuccess($"Customer '{target.Name}' deactivated successfully.");
                        }
                        catch (Exception ex)
                        {
                            _notificationService.ShowError($"Failed to deactivate customer: {ex.Message}");
                        }
                    }
                    await LoadCustomersAsync();
                }
            }
        }
    }
}
