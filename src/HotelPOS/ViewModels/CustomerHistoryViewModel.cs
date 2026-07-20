using CommunityToolkit.Mvvm.ComponentModel;
using HotelPOS.Application.DTOs.Customer;
using HotelPOS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class CustomerHistoryViewModel : ObservableObject
    {
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private int _totalOrders;

        [ObservableProperty]
        private decimal _totalSpent;

        [ObservableProperty]
        private DateTime? _firstOrderDate;

        [ObservableProperty]
        private DateTime? _lastOrderDate;

        public ObservableCollection<CustomerOrderSummaryDto> Orders { get; } = new();

        public CustomerHistoryViewModel(ICustomerService customerService, INotificationService notificationService)
        {
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(customerService);
                App.RegisterTestService(notificationService);
            }
        }

        public async Task LoadAsync(int customerId)
        {
            using var scope = App.CreateDbScope();
            var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
            try
            {
                var history = await customerService.GetCustomerHistoryAsync(customerId);
                CustomerName = history.CustomerName;
                TotalOrders = history.TotalOrders;
                TotalSpent = history.TotalSpent;
                FirstOrderDate = history.FirstOrderDate;
                LastOrderDate = history.LastOrderDate;

                Orders.Clear();
                foreach (var order in history.Orders)
                    Orders.Add(order);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load customer history: {ex.Message}");
            }
        }
    }
}
